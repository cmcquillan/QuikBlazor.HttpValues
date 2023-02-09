using QuikBlazor.HttpValues.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace QuikBlazor.HttpValues;

public abstract class HttpValueComponentBase : ComponentBase
{
    [Inject]
    internal ILogger<HttpValueComponentBase> Logger { get; set; } = null!;

    [Inject]
    private HttpDataProvider DataProvider { get; set; } = null!;

    [Inject]
    private Debouncer Debouncer { get; set; } = null!;

    protected virtual Task OnErrorAsync(HttpValueErrorState errorState, HttpResponseMessage? httpResponse)
    {
        return Task.CompletedTask;
    }

    internal async Task<UrlKey> FireHttpRequest(RequestParameters parameters, RequestEvents events, bool forceFire = false)
    {
        CancellationTokenSource? source = null;

        return await Debouncer.Debounce(parameters.DebounceMilliseconds ?? 0, () =>
        {
            if (parameters.TimeoutMilliseconds.HasValue)
            {
                source = new CancellationTokenSource(parameters.TimeoutMilliseconds.Value);
                source.Token.Register(async () =>
                {
                    await DataProvider.CancelHttpRequest(parameters, parameters.InputAttributes ?? new());
                });
            }

            return DataProvider.FireHttpRequest(
               parameters,
               attributes: parameters.InputAttributes ?? new(),
               requestEvents: events);
        }) ?? new UrlKey("", "", null);
    }
}