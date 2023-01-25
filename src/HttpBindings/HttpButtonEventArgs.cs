namespace HttpBindings;

public class HttpButtonEventArgs<TValue> : EventArgs
{
    internal HttpButtonEventArgs(TValue? value, HttpValueErrorState errorState)
    {
        Value = value;
        ErrorState = errorState;
    }

    public TValue? Value { get; }

    public HttpValueErrorState ErrorState { get; }
}
