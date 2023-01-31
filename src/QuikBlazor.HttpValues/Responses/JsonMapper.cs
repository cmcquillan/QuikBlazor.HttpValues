using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace QuikBlazor.HttpValues.Responses;

public class ParseableMapper : IResponseMapper
{
    //ConcurrentDictionary<Type, >

    public bool CanMap(Type resultType, HttpResponseMessage responseMessage)
    {
        var names = resultType.GetInterfaces().Select(p => p.Name).ToArray();

        if (resultType.GetInterface("IParsable`1") is Type parseable)
        {
            if (parseable.GetMethod("TryParse") is MethodInfo method
                && method.IsStatic)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<object?> Map(Type resultType, HttpResponseMessage responseMessage)
    {
        if (resultType.GetInterface("IParsable`1") is Type parseable)
        {
            if (parseable.GetMethod("TryParse") is MethodInfo method
                && method.IsStatic)
            {
                var data = await responseMessage.Content.ReadAsStringAsync();

                var parameters = new object?[] { data, null };
                var result = (bool)method.Invoke(null, parameters)!;

                if (result)
                {
                    return Convert.ChangeType(parameters[1], resultType);
                }
            }
        }

        return null;
    }
}


public class JsonMapper : IResponseMapper
{
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public bool CanMap(Type resultType, HttpResponseMessage responseMessage)
    {
        return resultType.IsClass
            && responseMessage.IsSuccessStatusCode
            && responseMessage.Content.Headers?.ContentType?.MediaType is "application/json" or "text/json";
    }

    public async Task<object?> Map(Type resultType, HttpResponseMessage responseMessage)
    {
        using var stream = await responseMessage.Content.ReadAsStreamAsync();

        if (stream is not null)
        {
            return await JsonSerializer.DeserializeAsync(stream, resultType, _serializerOptions);
        }

        return default;
    }
}