using System.Text;

namespace QuikBlazor.HttpValues.Internal;

internal static class UrlParser
{
    internal static (string, UrlKey) ResolveUrlParameters(string urlTemplate, Dictionary<string, object?> attrs, Dictionary<string, object?> keyAttrs)
    {
        var key = new UrlKey("__url", urlTemplate, null);

        foreach (var kv in keyAttrs)
            key = key.Push(kv.Key, new UrlValue(kv.Value));

        StringBuilder newUrl = new(urlTemplate.Length);
        int paramStartIx = -1;
        int paramEndIx = -1;
        int lastParamEndIx = 0;


        for (int i = 0; i < urlTemplate.Length; i++)
        {
            if (urlTemplate[i] == '{')
            {
                paramStartIx = i;
            }
            else if (urlTemplate[i] == '}' && paramStartIx > -1)
            {
                paramEndIx = i;
            }

            if (paramStartIx > -1 && paramEndIx > paramStartIx)
            {
                newUrl.Append(urlTemplate.AsSpan(lastParamEndIx, paramStartIx - lastParamEndIx));

                var paramName = urlTemplate.Substring(paramStartIx + 1, paramEndIx - paramStartIx - 1);

                attrs.TryGetValue(paramName, out var paramValue);
                newUrl.Append(paramValue);
                key = key.Push(paramName, new UrlValue(paramValue));

                lastParamEndIx = paramEndIx + 1;
                paramStartIx = -1;
                paramEndIx = -1;
            }
        }

        if (urlTemplate.Length > lastParamEndIx)
        {
            newUrl.Append(urlTemplate.AsSpan(lastParamEndIx, urlTemplate.Length - lastParamEndIx));
        }

        return (newUrl.ToString(), key);
    }
}
