using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HttpBindings;

public enum HttpMethod
{
    Get,
    Post,
    Put,
    Delete,
    Head,
    Options,
}

public enum CascadingHttpValueErrorState
{
    None,
    HttpError,
    Timeout
}

public abstract partial class CascadingHttpValueBase<TValue> : ComponentBase
{
    private string? _httpClientName;
    private string? _resolvedUrl;
    private bool _isLoading;
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private Task<HttpResponseMessage?>? _responseTask;
    private CancellationTokenSource? _cancellationTokenSource;


    [Inject]
    private IHttpClientFactory? ClientFactory { get; set; }

    protected CascadingHttpValueErrorState ErrorState { get; set; } = CascadingHttpValueErrorState.None;

    [Parameter]
    public string? HttpClientName { get; set; }

    [Parameter]
    public HttpMethod Method { get; set; }

    [Parameter]
    public string? Url { get; set; }

    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    [Parameter]
    public RenderFragment<TValue?>? ChildContent { get; set; }

    [Parameter]
    public string? ContentType { get; set; }

    [Parameter]
    public RenderFragment? RequestBody { get; set; }

    [Parameter]
    public int? TimeoutMilliseconds { get; set; }

    [Parameter]
    public int? DebounceMilliseconds { get; set; }

    [Parameter]
    public RenderFragment? ErrorTemplate { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> InputAttributes { get; set; } = new();

    public HttpClient? Client { get; set; }

    protected HttpResponseMessage? ErrorResponse { get; private set; }

    public TValue? Result { get; set; }

    protected override Task OnInitializedAsync()
    {
        _isLoading = true;

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
        if (_responseTask is not null
            && _responseTask.IsCompleted)
        {
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }

        if (Url is not null)
        {
            _resolvedUrl = UrlParser.ResolveUrlParameters(Url, InputAttributes);
        }

        if (Client is not null)
        {
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

                return Client.SendAsync(request);
            }, DebounceMilliseconds, token).ContinueWith(async (httpTask) =>
            {
                if (!httpTask.IsCanceled)
                {
                    var response = await httpTask;
                    if (response is not null)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            ErrorState = CascadingHttpValueErrorState.None;
                            Result = await JsonSerializer.DeserializeAsync<TValue>(await response.Content.ReadAsStreamAsync(token), _serializerOptions);
                        }
                        else
                        {
                            ErrorState = CascadingHttpValueErrorState.HttpError;
                            ErrorResponse = response;
                        }

                        _isLoading = false;
                        return response;
                    }
                }

                if (httpTask.IsCanceled)
                {
                    ErrorState = CascadingHttpValueErrorState.Timeout;
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
        using var builder = new RenderTreeBuilder();
        RequestBody?.Invoke(builder);
        var frames = builder.GetFrames();

        StringBuilder str = new();
#pragma warning disable BL0006 // Do not use RenderTree types
        for (int i = 0; i < frames.Count; i++)
        {
            switch (frames.Array[i].FrameType)
            {
                case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Text:
                    str.Append(frames.Array[i].TextContent);
                    break;
                case Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Markup:
                    str.Append(frames.Array[i].MarkupContent);
                    break;
                default:
                    throw new NotSupportedException("Only text content is supported within the RequestBody template.");
            }
        }
#pragma warning restore BL0006 // Do not use RenderTree types

        var content = str.ToString();
        return new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType!));
    }
}
