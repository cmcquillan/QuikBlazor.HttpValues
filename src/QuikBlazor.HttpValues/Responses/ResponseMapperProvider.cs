namespace QuikBlazor.HttpValues.Responses;

internal class ResponseMapperProvider
{
    private readonly IEnumerable<IResponseMapper> _mappers;

    public ResponseMapperProvider(IEnumerable<IResponseMapper> mappers)
    {
        _mappers = mappers;
    }

    internal IResponseMapper? GetProvider(Type resultType, HttpResponseMessage responseMessage)
    {
        return _mappers.FirstOrDefault(p => p.CanMap(resultType, responseMessage));
    }
}
