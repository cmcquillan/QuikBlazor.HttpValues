using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace QuikBlazor.HttpValues.Internal;

internal record class HttpResult(HttpResponseMessage? Response, Exception? Exception);

internal class HttpData
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

    /*
     * Source that controls cancellation of the http request if all
     * objects that desire that request data have unregistered their
     * interest in the result.
     */
    private CancellationTokenSource _cts = new();

    /*
     * This TCS will wrap _responseMessage with the result from
     * data deserialization.
     */
    private TaskCompletionSource<HttpResult> _responseResultSource = new();

    private List<RegistrationValue> _registrationValues = new();

    internal Task<HttpResult> GetTask() => _responseResultSource.Task;

    /*
     * Initialization fields. Everything else is managed internally;
     */
    private readonly HttpClient _httpClient;
    private readonly HttpRequestMessage _httpRequestMessage;
    private readonly ILogger<HttpDataProvider> _logger;

    internal HttpData(HttpClient httpClient, HttpRequestMessage httpRequestMessage, ILogger<HttpDataProvider> logger)
    {
        _httpClient = httpClient;
        _httpRequestMessage = httpRequestMessage;
        _logger = logger;
    }

    internal int ReferenceCount => _registrationValues.Sum(p => p.Count);

    internal Task<HttpResult> FireHttpRequest()
    {
        if (TaskHasStarted(_responseResultSource.Task))
        {
            return _responseResultSource.Task;
        }

        Task.Run(() => SendHttpRequest(_httpRequestMessage, _cts.Token));

        return _responseResultSource.Task;
    }

    private bool TaskHasStarted(Task<HttpResult> task)
    {
        _logger.LogInformation("[Check] TaskHasStarted: {status}", task.Status);
        return task.Status != TaskStatus.WaitingForActivation;
    }

    internal bool IsCompleted => GetTask().IsCompleted;

    private async Task<HttpResult> SendHttpRequest(HttpRequestMessage request, CancellationToken token)
    {
        try
        {
            await _lock.WaitAsync();

            var response = await _httpClient.SendAsync(request, token);
            HttpResult? httpResult = null;

            if (response.IsSuccessStatusCode)
            {
                httpResult = new HttpResult(response, null);
            }
            else
            {
                httpResult = new HttpResult(response, null);
            }

            _responseResultSource.TrySetResult(httpResult);
            return httpResult;
        }
        catch (Exception ex)
        {
            var httpResult = new HttpResult(null, ex);
            _responseResultSource.TrySetResult(httpResult);
            return httpResult;
        }
        finally
        {
            _lock.Release();
            request?.Dispose();
        }
    }

    internal void Register(object source)
    {
        lock (_registrationValues)
        {
            RegistrationValue? value = _registrationValues.FirstOrDefault(p => ReferenceEquals(source, p.Reference));

            if (value.HasValue)
            {
                value?.Increment();
            }
            else
            {
                _registrationValues.Add(new RegistrationValue(source));
            }
        }
    }

    internal void Unregister(object source)
    {
        lock (_registrationValues)
        {
            _registrationValues.RemoveAll(p => p.Reference is null);

            RegistrationValue? value = _registrationValues.FirstOrDefault(p => ReferenceEquals(source, p.Reference));
            value?.Decrement();

            if (_registrationValues.Count == 0 || _registrationValues.All(p => p.Empty))
            {
                _cts?.Cancel();
                _responseResultSource?.TrySetCanceled();
            }
        }
    }

    private struct RegistrationValue
    {
        private WeakReference _reference;
        private int _count;

        public RegistrationValue(object reference)
        {
            _reference = new WeakReference(reference);
            _count = 1;
        }

        internal object? Reference => _reference.Target;

        internal void Increment() => Interlocked.Increment(ref _count);

        internal void Decrement() => Interlocked.Decrement(ref _count);

        internal bool Empty => _count <= 0;

        internal int Count => _count;
    }
}
