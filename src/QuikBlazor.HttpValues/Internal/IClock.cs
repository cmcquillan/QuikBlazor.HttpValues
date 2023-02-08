namespace QuikBlazor.HttpValues.Internal;

internal interface IClock
{
    DateTime Now { get; }
}
