namespace HttpBindings.Tests;

public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public TestHttpClientFactory(HttpClient httpClient) => _httpClient = httpClient;

    public HttpClient CreateClient(string name) => _httpClient;
}
