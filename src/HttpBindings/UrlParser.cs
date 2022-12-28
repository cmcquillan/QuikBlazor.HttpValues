using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpBindings;

internal static class UrlParser
{
    internal static string ResolveUrlParameters(string urlFormat, IDictionary<string, object> attrs)
    {
        StringBuilder newUrl = new(urlFormat.Length);
        int paramStartIx = -1;
        int paramEndIx = -1;
        int lastParamEndIx = 0;

        for (int i = 0; i < urlFormat.Length; i++)
        {
            if (urlFormat[i] == '{')
            {

                paramStartIx = i;
            }
            else if (urlFormat[i] == '}' && paramStartIx > -1)
            {
                paramEndIx = i;
            }

            if (paramStartIx > -1 && paramEndIx > paramStartIx)
            {
                newUrl.Append(urlFormat.AsSpan(lastParamEndIx, paramStartIx - lastParamEndIx));

                var paramName = urlFormat.Substring(paramStartIx + 1, paramEndIx - paramStartIx - 1);

                attrs.TryGetValue(paramName, out var paramValue);
                newUrl.Append(paramValue);

                lastParamEndIx = paramEndIx + 1;
                paramStartIx = -1;
                paramEndIx = -1;
            }
        }

        if (urlFormat.Length > lastParamEndIx)
        {
            newUrl.Append(urlFormat.AsSpan(lastParamEndIx, urlFormat.Length - lastParamEndIx));
        }

        return newUrl.ToString();
    }
}
