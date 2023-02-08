using System.Collections.Concurrent;

namespace QuikBlazor.HttpValues.Internal;

internal class RequestCache
{
    private readonly ConcurrentDictionary<UrlKey, HttpData> _requestCache = new();

    internal bool TryGetData(UrlKey urlKey, out HttpData? data)
    {
        if (_requestCache.TryGetValue(urlKey, out data))
        {
            if (data.IsCompleted)
            {
                _requestCache.TryRemove(urlKey, out _);
                return false;
            }

            return true;
        }

        return false;
    }

    internal HttpData Add(UrlKey urlKey, Func<HttpData> createCallback)
    {
        TryGetData(urlKey, out _);

        return _requestCache.GetOrAdd(urlKey, (key) => createCallback());
    }
}
