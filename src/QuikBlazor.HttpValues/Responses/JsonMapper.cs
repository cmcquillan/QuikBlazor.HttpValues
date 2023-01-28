using System.Text.Json;

namespace QuikBlazor.HttpValues.Responses;

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