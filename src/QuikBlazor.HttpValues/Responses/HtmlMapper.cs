using Microsoft.AspNetCore.Components;

namespace QuikBlazor.HttpValues.Responses;

public class HtmlMapper : IResponseMapper
{
    private static readonly Type _strType = typeof(string);
    private static readonly Type _markupStrType = typeof(MarkupString);

    public bool CanMap(Type resultType, HttpResponseMessage responseMessage) => resultType == _strType || resultType == _markupStrType;

    public async Task<object?> Map(Type resultType, HttpResponseMessage responseMessage)
    {
        using var stream = await responseMessage.Content.ReadAsStreamAsync();

        if (stream is not null)
        {
            using var reader = new StreamReader(stream);
            var strResponse = await reader.ReadToEndAsync();
            return resultType == _strType ? strResponse : (MarkupString)strResponse;
        }

        return default;
    }
}
