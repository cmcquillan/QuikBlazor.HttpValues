using QuikBlazor.HttpValues.Internal;
using QuikBlazor.HttpValues.Responses;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace QuikBlazor.HttpValues;

public abstract class HttpValueBase<TValue> : ComponentBase, IHttpValueAwaitable<TValue>
{
    private string? _httpClientName;
    private string? _resolvedUrl;
    private UrlKey _urlKey = new("", "", null);
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationTokenSource? _previousTokenSource;
    private Task<HttpResponseMessage?>? _responseTask;
    private TaskCompletionSource<TValue?>? _awaitableTaskSource;
    private JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Parameter]
    public string? HttpClientName { get; set; }

    [Parameter]
    public string? ContentType { get; set; } = "application/json";

    [Parameter]
    public int? DebounceMilliseconds { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> InputAttributes { get; set; } = new();

    [Parameter]
    public HttpMethod Method { get; set; }

    [Parameter]
    public object? RequestBody { get; set; }

    [Parameter]
    public int? TimeoutMilliseconds { get; set; }

    [Parameter]
    public string? Url { get; set; }

    [Inject]
    private IHttpClientProvider ClientProvider { get; set; } = null!;

    [Inject]
    private ResponseMapperProvider MapperProvider { get; set; } = null!;

    public HttpValueErrorState ErrorState { get; private set; } = HttpValueErrorState.None;

    protected HttpClient? Client { get; set; }

    protected TValue? Result { get; set; }

    protected HttpResponseMessage? ErrorResponse { get; private set; }

    protected abstract Task OnNewValueAsync(TValue? value);

    protected virtual Task OnErrorAsync(HttpValueErrorState errorState, HttpResponseMessage? httpResponse)
    {
        return Task.CompletedTask;
    }

    protected async Task FireHttpRequest(bool forceFire = false)
    {
        UpdateHttpClientState();
        InitializeAndGetCompletionSource();

        if (Url is not null)
        {
            var attrs = new Dictionary<string, object?>(InputAttributes!);

            foreach (var attr in InputAttributes)
            {
                if (attr.Value is Task t)
                {
                    if (!t.IsCompleted)
                        await t;

                    attrs[attr.Key] = t.GetType().GetProperty("Result")?.GetValue(t);
                }
            }

            (string? newUrl, UrlKey? newKey) = UrlParser.ResolveUrlParameters(Url, attrs, new()
            {
                { "__method", Method },
                { "__body", Method is HttpMethod.Post or HttpMethod.Put ? RequestBody : null }
            });

            if (newKey.Equals(_urlKey) && !forceFire)
            {
                CompleteAndClearCompletionSource(s => s.TrySetResult(Result));
                return;
            }

            _resolvedUrl = newUrl;
            _urlKey = newKey;
        }

        if (_responseTask is not null && _responseTask.IsCompleted)
        {
            if (_cancellationTokenSource is not null)
            {
                CompleteAndClearCompletionSource(s => s.TrySetResult(Result));
                Debug.WriteLine("Cancelling current token");
                _previousTokenSource = _cancellationTokenSource;
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }

        if (Client is not null)
        {
            Debug.WriteLine("Constructing cancellation token from {0}ms", TimeoutMilliseconds);
            _cancellationTokenSource = TimeoutMilliseconds.HasValue
                ? new CancellationTokenSource(TimeoutMilliseconds.Value)
                : new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;

            if (DebounceMilliseconds.HasValue)
            {
                await Task.Delay(DebounceMilliseconds.Value);
            }

            if (token.IsCancellationRequested)
            {
                // Likely a debounce since we haven't initiated an http request yet
                return;
            }

            // Build our request
            var request = new HttpRequestMessage(Method switch
            {
                HttpMethod.Get => System.Net.Http.HttpMethod.Get,
                HttpMethod.Post => System.Net.Http.HttpMethod.Post,
                _ => throw new NotSupportedException(),
            }, _resolvedUrl);

            if (Method is HttpMethod.Post or HttpMethod.Put)
            {
                request.Content = GetRequestBody();
            }

            // Start an http request.
            _responseTask = Task.Run(() => SendHttpRequest(request, token));
        }
    }

    private void UpdateHttpClientState()
    {
        if (_httpClientName == HttpClientName && Client is not null)
        {
            // No change to client.
            return;
        }

        if (_httpClientName is null && HttpClientName is null && Client is not null)
        {
            // No change to client.
            return;
        }

        _httpClientName = HttpClientName;

        Client = ClientProvider.GetHttpClient(_httpClientName);
    }

    private async Task<HttpResponseMessage?> SendHttpRequest(HttpRequestMessage request, CancellationToken token)
    {
        try
        {
            var response = await Client!.SendAsync(request, token);
            if (response is not null)
            {
                var responseType = typeof(TValue);
                if (response.IsSuccessStatusCode && MapperProvider.GetProvider(responseType, response) is { } mapper)
                {
                    ErrorState = HttpValueErrorState.None;

                    Result = (TValue?)await mapper.Map(responseType, response);
                    await OnNewValueAsync(Result);
                    CompleteAndClearCompletionSource(s => s.TrySetResult(Result));
                }
                else
                {
                    ErrorState = HttpValueErrorState.HttpError;
                    ErrorResponse = response;
                }

                return response;
            }
        }
        catch (TaskCanceledException ex)
        {
            ErrorState = HttpValueErrorState.Timeout;
            CompleteAndClearCompletionSource(s => s.TrySetException(ex));
        }
        catch (Exception ex)
        {
            ErrorState = HttpValueErrorState.Exception;
            CompleteAndClearCompletionSource(s => s.TrySetException(ex));
        }
        finally
        {
            StateHasChanged();
            request?.Dispose();
        }

        return null;
    }

    private HttpContent? GetRequestBody()
    {
        var content = JsonSerializer.Serialize(RequestBody, _serializerOptions);
        return new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType!));
    }

    public TaskAwaiter<TValue?> GetAwaiter()
    {
        var source = InitializeAndGetCompletionSource();
        return source.Task.GetAwaiter();
    }

    private TaskCompletionSource<TValue?> InitializeAndGetCompletionSource()
    {
        return _awaitableTaskSource ??= new TaskCompletionSource<TValue?>();
    }

    private void CompleteAndClearCompletionSource(Action<TaskCompletionSource<TValue?>> callback)
    {
        if (_awaitableTaskSource is not null)
            callback(_awaitableTaskSource);

        _awaitableTaskSource = null;
    }
}