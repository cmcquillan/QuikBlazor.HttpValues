using QuikBlazor.HttpValues.Responses;
using Microsoft.Extensions.DependencyInjection;
using QuikBlazor.HttpValues.Internal;

namespace QuikBlazor.HttpValues;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddHttpValues(this IServiceCollection services)
    {
        services.AddSingleton<ResponseMapperProvider>();
        services.AddSingleton<IResponseMapper, JsonMapper>();
        services.AddSingleton<IResponseMapper, HtmlMapper>();
        services.AddSingleton<IResponseMapper, ParseableMapper>();

        services.AddSingleton<RequestMapperProvider>();
        services.AddSingleton<IRequestBodyMapper, JsonMapper>();

        services.AddTransient<Debouncer>();
        services.AddSingleton<IClock, Clock>();

        services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        services.AddSingleton<ChangeSignalService>();
        services.AddSingleton<HttpDataProvider>();
        return services;
    }
}
