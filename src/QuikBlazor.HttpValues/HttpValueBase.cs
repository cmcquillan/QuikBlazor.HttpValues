using QuikBlazor.HttpValues.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace QuikBlazor.HttpValues;

public abstract class HttpValueBase<TValue> : HttpValueComponentBase, ISignalReceiver
{
    private const string DefaultContentType = "application/json";
    private UrlKey _urlKey = new("", "", null);

    [Parameter]
    public string? HttpClientName { get; set; }

    [Parameter]
    public string? ContentType { get; set; } = DefaultContentType;

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

    public HttpValueErrorState ErrorState { get; private set; } = HttpValueErrorState.None;

    protected HttpResponseMessage? ErrorResponse { get; private set; }

    protected TValue? Result { get; private set; }

    protected override void OnInitialized()
    {
        SignalService.RegisterForSignal(this);
    }

    protected async Task FireHttpRequest()
    {
        ArgumentNullException.ThrowIfNull(Url);

        var parameters = new RequestParameters
        {
            ResponseType = typeof(TValue),
            Source = this,
            PriorKey = _urlKey,
            UrlTemplate = Url,
            Method = Method,
            RequestBody = RequestBody,
            ContentType = ContentType ?? DefaultContentType,
            HttpClientName = HttpClientName,
            DebounceMilliseconds = DebounceMilliseconds,
            TimeoutMilliseconds = TimeoutMilliseconds,
            InputAttributes = InputAttributes,
        };

        var events = new RequestEvents
        {
            OnSuccess = (val, resp) => OnRequestSuccess(val, resp),
            OnError = OnRequestError,
        };

        _urlKey = await FireHttpRequest(parameters, events);
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

    private Task OnRequestSuccess(object? result, HttpResponseMessage response)
    {
        var typedResult = (TValue?)result;
        return InvokeAsync(async () =>
        {
            Result = typedResult;
            await OnNewValueAsync(typedResult);
            StateHasChanged();
        });
    }

    protected abstract Task OnNewValueAsync(TValue? value);

    public async Task FireRequest()
    {
        await FireHttpRequest();
    }

    async Task ISignalReceiver.Signal(object sender)
    {
        Logger.LogInformation("Received Signal");
        await FireHttpRequest();
        Logger.LogInformation("Completed Signal on {0}", Url);
    }
}
