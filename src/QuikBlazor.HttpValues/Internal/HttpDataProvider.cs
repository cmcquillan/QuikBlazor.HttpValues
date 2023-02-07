using QuikBlazor.HttpValues.Responses;
using System.Collections.Concurrent;

namespace QuikBlazor.HttpValues.Internal;

internal class HttpDataProvider
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IClock _clock;
    private readonly ResponseMapperProvider _responseMapperProvider;
    private readonly RequestMapperProvider _requestMapperProvider;
    private readonly TemplateCache _templateCache = new();
    private readonly ConcurrentDictionary<HttpData, object?> _intermediateCache = new();

    public HttpDataProvider(
        IHttpClientProvider httpClientProvider,
        IClock clock,
        ResponseMapperProvider responseMapperProvider,
        RequestMapperProvider requestMapperProvider)
    {
        _httpClientProvider = httpClientProvider;
        _clock = clock;
        _responseMapperProvider = responseMapperProvider;
        _requestMapperProvider = requestMapperProvider;
    }

    internal async Task CancelHttpRequest(RequestParameters parameters, IDictionary<string, object?> attributes)
    {
        (var url, var key) = await GenerateUrlKey(
            parameters.UrlTemplate,
            parameters.Method,
            parameters.RequestBody, attributes);

        if (_templateCache.TryGetRequestData(url, key, out var httpData))
        {
            httpData.Unregister(parameters.Source);
        }
    }

    internal async Task<UrlKey> FireHttpRequest<TValue>(
        RequestParameters parameters,
        IDictionary<string, object?> attributes,
        RequestEvents<TValue> requestEvents)
    {
        try
        {
            (var url, var key) = await GenerateUrlKey(
                parameters.UrlTemplate,
                parameters.Method,
                parameters.RequestBody, attributes);

            if (key == parameters.PriorKey)
            {
                return key;
            }

            var httpData = _templateCache.GetOrCreate(url, key, () =>
            {
                var client = _httpClientProvider.GetHttpClient(parameters.HttpClientName);

                // Build our request
                var request = new HttpRequestMessage(parameters.Method switch
                {
                    HttpMethod.Get => System.Net.Http.HttpMethod.Get,
                    HttpMethod.Post => System.Net.Http.HttpMethod.Post,
                    _ => throw new NotSupportedException(),
                }, url);

                if (parameters.Method is HttpMethod.Post or HttpMethod.Put)
                {
                    request.Content = GetRequestBody(parameters.ContentType, parameters.RequestBody);
                }

                return new HttpData(client, request);
            });

            httpData.Register(parameters.Source);

            (var response, var exception) = await httpData.FireHttpRequest();
            Task? awaitable = null;
            switch (exception)
            {
                case null when response is { IsSuccessStatusCode: true }:

                    if (_intermediateCache.TryGetValue(httpData, out var cachedResult))
                    {
                        awaitable = requestEvents.OnSuccess?.Invoke((TValue?)cachedResult, response);
                        break;
                    }

                    var resultType = typeof(TValue);
                    var mapper = _responseMapperProvider.GetProvider(resultType, response);

                    if (mapper is not null)
                    {
                        var result = (TValue?)await mapper.Map(resultType, response);
                        _intermediateCache.TryAdd(httpData, result);
                        awaitable = requestEvents.OnSuccess?.Invoke(result, response);
                    }
                    break;
                case null:
                    awaitable = requestEvents?.OnError?.Invoke(HttpValueErrorState.HttpError, null, response);
                    break;
                case OperationCanceledException oex:
                    awaitable = requestEvents.OnError?.Invoke(HttpValueErrorState.Timeout, oex, response);
                    break;
                case Exception ex:
                    awaitable = requestEvents.OnError?.Invoke(HttpValueErrorState.Exception, ex, response);
                    break;
            }

            if (awaitable is not null)
            {
                await awaitable;
            }

            return key;
        }
        finally
        {
            /*
             * Get the http data for hte previous key for this source. This is so that 
             * we can unregister the source since we are no longer interested in this data.
             */
            if (parameters.PriorKey is not null
                && _templateCache.TryGetRequestData(
                    parameters.UrlTemplate, parameters.PriorKey, out var previousRequestData))
            {
                previousRequestData?.Unregister(parameters.Source);
            }
        }
    }

    private HttpContent? GetRequestBody(string contentType, object? requestBody)
    {
        if (requestBody is null)
            return null;

        var type = requestBody?.GetType();
        if (type is not null && _requestMapperProvider.GetProvider(type, contentType, requestBody) is { } mapper)
        {
            return mapper.Map(type, contentType, requestBody);
        }

        return null;
    }

    private static async Task<(string url, UrlKey key)> GenerateUrlKey(string urlTemplate, HttpMethod method, object? requestBody, IDictionary<string, object?> attributes)
    {
        var attrs = new Dictionary<string, object?>(attributes);

        foreach (var attr in attributes)
        {
            if (attr.Value is Task t)
            {
                if (!t.IsCompleted)
                    await t;

                attrs[attr.Key] = t.GetType().GetProperty("Result")?.GetValue(t);
            }
        }

        return UrlParser.ResolveUrlParameters(urlTemplate, attrs, new()
            {
                { "__method", method },
                { "__body", method is HttpMethod.Post or HttpMethod.Put ? requestBody : null }
            });
    }
}
