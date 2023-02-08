namespace QuikBlazor.HttpValues.Internal;

internal delegate Task OnSuccess(object? value, HttpResponseMessage response);
internal delegate Task OnError(HttpValueErrorState errorState, Exception? exception, HttpResponseMessage? response);

internal class RequestEvents
{
    public OnSuccess? OnSuccess { get; set; }

    public OnError? OnError { get; set; }
}