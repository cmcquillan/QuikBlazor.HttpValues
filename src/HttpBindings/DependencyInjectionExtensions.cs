using HttpBindings.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace HttpBindings;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddHttpBindings(this IServiceCollection services)
    {
        services.AddSingleton<ResponseMapperProvider>();
        services.AddSingleton<IResponseMapper, JsonMapper>();
        services.AddSingleton<IResponseMapper, HtmlMapper>();
        services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        return services;
    }
}
