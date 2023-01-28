namespace QuikBlazor.HttpValues.Internal;

internal struct UrlValue : IEquatable<UrlValue>
{
    private readonly object? value;

    internal bool IsNull => value is null;

    public UrlValue(object? value)
    {
        this.value = value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UrlValue value && Equals(value);
    }

    public bool Equals(UrlValue other)
    {
        if (IsNull && other.IsNull) return true;

        return EqualityComparer<object?>.Default.Equals(value, other.value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value);
    }

    public static bool operator ==(UrlValue left, UrlValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UrlValue left, UrlValue right)
    {
        return !(left == right);
    }
}
