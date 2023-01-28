using Microsoft.Extensions.DependencyInjection;

namespace QuikBlazor.HttpValues;

public class HttpClientProvider : IHttpClientProvider
{
    private readonly IHttpClientFactory? _httpClientFactory;

    public HttpClientProvider(IServiceProvider serviceProvider)
    {
        _httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
    }

    public HttpClient GetHttpClient(string? httpClientName)
    {
        return (httpClientName, _httpClientFactory) switch
        {
            (not null, null) => throw new NotSupportedException("Cannot use IHttpClientFactory"),
            (null, null) => new HttpClient(),
            (null, not null) => _httpClientFactory.CreateClient(),
            (not null, not null) => _httpClientFactory.CreateClient(httpClientName),
        };
    }
}
