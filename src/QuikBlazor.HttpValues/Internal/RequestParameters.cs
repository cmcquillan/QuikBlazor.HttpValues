namespace QuikBlazor.HttpValues.Internal;

internal class RequestParameters
{
    internal object Source { get; set; } = null!;

    internal UrlKey? PriorKey { get; set; }

    internal string UrlTemplate { get; set; } = null!;

    internal HttpMethod Method { get; set; }

    internal object? RequestBody { get; set; }

    internal string ContentType { get; set; } = null!;

    internal string? HttpClientName { get; set; } = null;
}
