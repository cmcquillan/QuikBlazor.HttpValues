using QuikBlazor.HttpValues.Internal;
using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace QuikBlazor.HttpValues;

public class HttpComponentBase<TValue> : HttpValueBase<TValue>
{
    protected override Task OnNewValueAsync(TValue? value)
    {
        throw new NotImplementedException();
    }
}

public abstract class HttpValueBase<TValue> : ComponentBase, IHttpValueAwaitable<TValue>, ISignalReceiver
{
    private const string DefaultContentType = "application/json";
    private UrlKey _urlKey = new("", "", null);
    private TaskCompletionSource<TValue?>? _awaitableTaskSource;

    [Parameter]
    public string? HttpClientName { get; set; }

    [Parameter]
    public string? ContentType { get; set; } = HttpValueBase<TValue>.DefaultContentType;

    [Parameter]
    public int? DebounceMilliseconds { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object?> InputAttributes { get; set; } = new();

    [Parameter]
    public HttpMethod Method { get; set; }

    [Parameter]
    public object? RequestBody { get; set; }

    [Parameter]
    public int? TimeoutMilliseconds { get; set; }

    [Parameter]
    public string? Url { get; set; }

    [Inject]
    private ChangeSignalService SignalService { get; set; } = null!;

    [Inject]
    private ILogger<HttpValueBase<TValue>> Logger { get; set; } = null!;

    [Inject]
    private HttpDataProvider DataProvider { get; set; } = null!;

    [Inject]
    private Debouncer Debouncer { get; set; } = null!;

    public HttpValueErrorState ErrorState { get; private set; } = HttpValueErrorState.None;

    protected HttpClient? Client { get; set; }

    protected TValue? Result { get; set; }

    protected HttpResponseMessage? ErrorResponse { get; private set; }

    protected override void OnInitialized()
    {
        SignalService.RegisterForSignal(this);
    }

    protected abstract Task OnNewValueAsync(TValue? value);

    protected virtual Task OnErrorAsync(HttpValueErrorState errorState, HttpResponseMessage? httpResponse)
    {
        return Task.CompletedTask;
    }

    protected async Task FireHttpRequest(bool forceFire = false)
    {
        if (Url is not null)
        {
            CancellationTokenSource? source = null;
            var parameters = new RequestParameters
            {
                Source = this,
                PriorKey = _urlKey,
                UrlTemplate = Url,
                Method = Method,
                RequestBody = RequestBody,
                ContentType = ContentType ?? DefaultContentType,
                HttpClientName = HttpClientName,
            };

            var events = new RequestEvents
            {
                OnSuccess = (val, resp) => OnRequestSuccess((TValue?)val, resp),
                OnError = OnRequestError,
            };

            _urlKey = await Debouncer.Debounce(DebounceMilliseconds ?? 0, () =>
            {
                if (TimeoutMilliseconds.HasValue)
                {
                    source = new CancellationTokenSource(TimeoutMilliseconds.Value);
                    source.Token.Register(async () =>
                    {
                        await DataProvider.CancelHttpRequest(parameters, InputAttributes);
                    });
                }

                return DataProvider.FireHttpRequest<TValue>(
                   parameters,
                   attributes: InputAttributes,
                   requestEvents: events);
            }) ?? new UrlKey("", "", null);
        }
    }

    private Task OnRequestError(HttpValueErrorState errorState, Exception? exception, HttpResponseMessage? response)
    {
        return InvokeAsync(async () =>
        {
            ErrorState = errorState;
            ErrorResponse = response;
            await OnErrorAsync(errorState, response);
            StateHasChanged();
        });
    }

    private Task OnRequestSuccess(TValue? result, HttpResponseMessage response)
    {
        return InvokeAsync(async () =>
        {
            Result = result;
            await OnNewValueAsync(result);
            StateHasChanged();
        });
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

    public async Task FireRequest()
    {
        await FireHttpRequest(forceFire: true);
    }

    async Task ISignalReceiver.Signal(object sender)
    {
        Logger.LogInformation("Received Signal");
        await FireHttpRequest();
        Logger.LogInformation("Completed Signal on {0}", Url);
    }
}