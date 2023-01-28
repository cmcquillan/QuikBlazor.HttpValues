using QuikBlazor.HttpValues.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace QuikBlazor.HttpValues;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddHttpValues(this IServiceCollection services)
    {
        services.AddSingleton<ResponseMapperProvider>();
        services.AddSingleton<IResponseMapper, JsonMapper>();
        services.AddSingleton<IResponseMapper, HtmlMapper>();
        services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        return services;
    }
}
