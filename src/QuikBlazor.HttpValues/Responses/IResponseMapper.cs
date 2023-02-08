namespace QuikBlazor.HttpValues.Responses;

public interface IResponseMapper
{
    bool CanMap(Type resultType, HttpResponseMessage responseMessage);

    Task<object?> Map(Type resultType, HttpResponseMessage responseMessage);
}

public interface IRequestBodyMapper
{
    bool CanMap(Type requestType, string contentType, object? requestBody);

    HttpContent? Map(Type requestType, string contentType, object? requestBody);
}