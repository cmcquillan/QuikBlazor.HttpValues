using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace QuikBlazor.HttpValues.Internal;

internal class TemplateCache
{
    private readonly ConcurrentDictionary<string, RequestCache> _requestCaches = new();

    internal bool TryGetRequestData(string urlTemplate, UrlKey urlKey, [NotNullWhen(true)] out HttpData? data)
    {
        if (_requestCaches.TryGetValue(urlTemplate, out var cache))
        {
            return cache.TryGetData(urlKey, out data);
        }

        data = null;
        return false;
    }

    internal HttpData GetOrCreate(string urlTemplate, UrlKey urlKey, Func<HttpData> createCallback)
    {
        RequestCache cache = _requestCaches.GetOrAdd(urlTemplate, (template) =>
        {
            var created = new RequestCache();
            return created;
        });

        return cache.Add(urlKey, createCallback);
    }
}
