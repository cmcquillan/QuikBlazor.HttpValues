namespace QuikBlazor.HttpValues.Responses;

internal class RequestMapperProvider
{
    private readonly IEnumerable<IRequestBodyMapper> _mappers;

    public RequestMapperProvider(IEnumerable<IRequestBodyMapper> mappers)
    {
        _mappers = mappers;
    }

    internal IRequestBodyMapper? GetProvider(Type expectedType, string contentType, object requestBody)
    {
        return _mappers.FirstOrDefault(p => p.CanMap(expectedType, contentType, requestBody));
    }
}