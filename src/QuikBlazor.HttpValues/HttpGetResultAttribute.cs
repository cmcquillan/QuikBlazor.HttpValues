namespace QuikBlazor.HttpValues;

[AttributeUsage(AttributeTargets.Property)]
public class HttpGetResultAttribute : Attribute
{
    public HttpGetResultAttribute(string url)
    {
        Url = url;
    }

    public string Url { get; set; }

    public string? HttpClient { get; set; }

    public int? TimeoutMilliseconds { get; set; }
}
