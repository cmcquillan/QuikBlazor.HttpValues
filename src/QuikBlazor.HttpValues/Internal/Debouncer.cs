namespace QuikBlazor.HttpValues.Internal;

internal class Debouncer
{
    private CancellationTokenSource _cts = new();

    internal async Task<T?> Debounce<T>(int debounceTime, Func<Task<T>> executionCallback)
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        await Task.Delay(debounceTime);

        if (token.IsCancellationRequested)
        {
            return default;
        }

        return await executionCallback();
    }
}
