using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HttpBindings;

public abstract class HttpValueBase<TValue> : ComponentBase
{
    private string? _httpClientName;
    private string? _resolvedUrl;
    private UrlKey _urlKey = new("", "", null);
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationTokenSource? _previousTokenSource;
    private Task<HttpResponseMessage?>? _responseTask;
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Parameter]
    public string? HttpClientName { get; set; }

    public HttpClient? Client { get; set; }

    [Parameter]
    public string? ContentType { get; set; }

    [Parameter]
    public int? DebounceMilliseconds { get; set; }

    public CascadingHttpValueErrorState ErrorState { get; private set; } = CascadingHttpValueErrorState.None;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> InputAttributes { get; set; } = new();

    [Parameter]
    public HttpMethod Method { get; set; }

    [Parameter]
    public object? RequestBody { get; set; }

    public TValue? Result { get; set; }

    [Parameter]
    public int? TimeoutMilliseconds { get; set; }

    [Parameter]
    public string? Url { get; set; }

    protected bool IsLoading { get; set; }

    protected HttpResponseMessage? ErrorResponse { get; private set; }

    [Inject]
    private IHttpClientFactory? ClientFactory { get; set; }

    protected abstract Task OnNewValueAsync(TValue? value);

    protected override Task OnInitializedAsync()
    {
        IsLoading = true;

        return base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        UpdateHttpClientState();

        void UpdateHttpClientState()
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

            Client = (_httpClientName, ClientFactory) switch
            {
                (not null, null) => throw new NotSupportedException("Cannot use IHttpClientFactory"),
                (null, null) => new HttpClient(),
                (null, not null) => ClientFactory.CreateClient(),
                (not null, not null) => ClientFactory.CreateClient(_httpClientName),
            };
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Url is not null)
        {
            (var newUrl, var newKey) = UrlParser.ResolveUrlParameters(Url, InputAttributes);

            if (newKey.Equals(_urlKey))
            {
                return;
            }

            _resolvedUrl = newUrl;
            _urlKey = newKey;
        }

        if (_responseTask is not null && _responseTask.IsCompleted)
        {
            if (_cancellationTokenSource is not null)
            {
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
            var task = await TaskHelper.Debounce(() =>
            {
                using var request = new HttpRequestMessage(Method switch
                {
                    HttpMethod.Get => System.Net.Http.HttpMethod.Get,
                    HttpMethod.Post => System.Net.Http.HttpMethod.Post,
                    _ => throw new NotSupportedException(),
                }, _resolvedUrl);

                if (Method is HttpMethod.Post or HttpMethod.Put)
                {
                    request.Content = GetRequestBody();
                }

                return Client.SendAsync(request, token);
            }, DebounceMilliseconds, token).ContinueWith(async (httpTask) =>
            {
                if (!httpTask.IsCanceled && !token.IsCancellationRequested)
                {
                    var response = await httpTask;
                    if (response is not null)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            ErrorState = CascadingHttpValueErrorState.None;
                            Result = await JsonSerializer.DeserializeAsync<TValue>(await response.Content.ReadAsStreamAsync(token), _serializerOptions);
                            await OnNewValueAsync(Result);
                        }
                        else
                        {
                            ErrorState = CascadingHttpValueErrorState.HttpError;
                            ErrorResponse = response;
                        }

                        IsLoading = false;
                        return response;
                    }
                }

                if (httpTask.IsCanceled || token.IsCancellationRequested)
                {
                    ErrorState = CascadingHttpValueErrorState.Timeout;
                    StateHasChanged();
                }

                if (httpTask.IsFaulted)
                {
                    var exception = httpTask.Exception;
                }

                return null;
            });

            _responseTask = task!;
        }
    }

    private HttpContent? GetRequestBody()
    {
        var content = JsonSerializer.Serialize(RequestBody, _serializerOptions);
        return new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType!));
        //        using var builder = new RenderTreeBuilder();
        //        RequestBody?.Invoke(builder);
        //        var frames = builder.GetFrames();

        //        StringBuilder str = new();
        //#pragma warning disable BL0006 // Do not use RenderTree types
        //        for (int i = 0; i < frames.Count; i++)
        //        {
        //            switch (frames.Array[i].FrameType)
        //            {
        //                case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Text:
        //                    str.Append(frames.Array[i].TextContent);
        //                    break;
        //                case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Markup:
        //                    str.Append(frames.Array[i].MarkupContent);
        //                    break;
        //                default:
        //                    throw new NotSupportedException("Only text content is supported within the RequestBody template.");
        //            }
        //        }
        //#pragma warning restore BL0006 // Do not use RenderTree types

        //        var content = str.ToString();

    }
}