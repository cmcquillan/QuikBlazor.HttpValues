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


public class JsonMapper : IResponseMapper, IRequestBodyMapper
{
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public bool CanMap(Type resultType, HttpResponseMessage responseMessage) => responseMessage.IsSuccessStatusCode
            && IsSerializeable(resultType, responseMessage.Content.Headers?.ContentType?.MediaType);

    public bool CanMap(Type requestType, string contentType, object? requestBody) => IsSerializeable(requestType, contentType);

    private bool IsSerializeable(Type type, string? contentType) => type.IsClass && contentType is "application/json" or "text/json";

    public async Task<object?> Map(Type resultType, HttpResponseMessage responseMessage)
    {
        using var stream = await responseMessage.Content.ReadAsStreamAsync();

        if (stream is not null)
        {
            return await JsonSerializer.DeserializeAsync(stream, resultType, _serializerOptions);
        }

        return default;
    }

    public HttpContent? Map(Type requestType, string contentType, object? requestBody)
    {
        var content = JsonSerializer.Serialize(requestBody, requestType, _serializerOptions);
        return new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue(contentType));
    }
}