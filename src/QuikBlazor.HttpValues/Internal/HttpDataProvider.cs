using Microsoft.Extensions.Logging;
using QuikBlazor.HttpValues.Responses;
using System.Collections.Concurrent;

namespace QuikBlazor.HttpValues.Internal;

internal class HttpDataProvider
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly ResponseMapperProvider _responseMapperProvider;
    private readonly RequestMapperProvider _requestMapperProvider;
    private readonly ILogger<HttpDataProvider> _logger;
    private readonly TemplateCache _templateCache = new();
    private readonly ConcurrentDictionary<HttpData, object?> _intermediateCache = new();

    public HttpDataProvider(
        IHttpClientProvider httpClientProvider,
        ResponseMapperProvider responseMapperProvider,
        RequestMapperProvider requestMapperProvider,
        ILogger<HttpDataProvider> logger)
    {
        _httpClientProvider = httpClientProvider;
        _responseMapperProvider = responseMapperProvider;
        _requestMapperProvider = requestMapperProvider;
        _logger = logger;
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

    internal async Task<UrlKey> FireHttpRequest(
        RequestParameters parameters,
        IDictionary<string, object?> attributes,
        RequestEvents requestEvents)
    {
        HttpResponseMessage? response = null;
        UrlKey? key = null;

        try
        {
            (var url, key) = await GenerateUrlKey(
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

                return new HttpData(client, request, _logger);
            });

            httpData.Register(parameters.Source);

            (response, var exception) = await httpData.FireHttpRequest();
            Task? awaitable = null;
            switch (exception)
            {
                case null when response is { IsSuccessStatusCode: true }:

                    if (_intermediateCache.TryGetValue(httpData, out var cachedResult))
                    {
                        awaitable = requestEvents.OnSuccess?.Invoke(cachedResult, response);
                        break;
                    }

                    var mapper = _responseMapperProvider.GetProvider(parameters.ResponseType, response);

                    if (mapper is not null)
                    {
                        var result = await mapper.Map(parameters.ResponseType, response);
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
        catch (OperationCanceledException oex)
        {
            if (requestEvents.OnError is not null)
            {
                await requestEvents.OnError.Invoke(HttpValueErrorState.Timeout, oex, response);
            }

            return key!;
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

        var type = requestBody.GetType();
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
