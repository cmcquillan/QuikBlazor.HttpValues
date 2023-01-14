namespace HttpBindings.Internal;

internal record class UrlKey(string Key, object? Value, UrlKey? Next)
{
    public UrlKey Push(string key, UrlValue value)
    {
        return new UrlKey(key, value, this);
    }
}
