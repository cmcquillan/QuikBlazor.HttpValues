namespace QuikBlazor.HttpValues.Internal;

internal class RequestEvents<TValue>
{
    internal Func<TValue?, HttpResponseMessage, Task>? OnSuccess { get; set; }

    internal Func<HttpValueErrorState, Exception?, HttpResponseMessage?, Task>? OnError { get; set; }
}