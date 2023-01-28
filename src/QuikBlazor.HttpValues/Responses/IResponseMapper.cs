namespace QuikBlazor.HttpValues.Responses;

public interface IResponseMapper
{
    bool CanMap(Type resultType, HttpResponseMessage responseMessage);

    Task<object?> Map(Type resultType, HttpResponseMessage responseMessage);
}
