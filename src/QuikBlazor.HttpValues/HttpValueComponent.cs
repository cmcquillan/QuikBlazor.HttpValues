using QuikBlazor.HttpValues.Internal;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Collections.Concurrent;

namespace QuikBlazor.HttpValues;

public abstract class HttpValueComponent : HttpValueComponentBase
{
    private record class HttpValueState(PropertyInfo Property, HttpGetResultAttribute Attribute, UrlKey Key);

    private readonly ConcurrentDictionary<string, HttpValueState> _cache = new();
    private Dictionary<string, object?> _parameters { get; set; } = new();

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object?> InputAttributes { get; set; } = new();

    protected override void OnInitialized()
    {
        var props = from prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    let attr = prop.GetCustomAttributes<HttpGetResultAttribute>().ToArray()
                    where attr.Length > 0
                    where prop.CanWrite && !prop.PropertyType.IsPrimitive
                    select (prop, attr);

        foreach ((var prop, var attr) in props)
        {
            _cache.TryAdd(prop.Name, new HttpValueState(prop, attr[0], new UrlKey("", "", null)));
        }
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var para in parameters)
        {
            if (!para.Cascading)
            {
                _parameters[para.Name] = para.Value;
                Console.WriteLine(nameof(para.Name) + ": " + para.Name);
            }
        }

        return base.SetParametersAsync(parameters);
    }

    protected override Task OnParametersSetAsync()
    {
        foreach (var values in _cache)
        {
            Console.WriteLine(nameof(OnParametersSetAsync) + ": " + values.Key);
            var parameters = new RequestParameters
            {
                ResponseType = values.Value.Property.PropertyType,
                Source = this,
                PriorKey = values.Value.Key,
                UrlTemplate = values.Value.Attribute.Url,
                Method = HttpMethod.Get,
                HttpClientName = values.Value.Attribute.HttpClient,
                TimeoutMilliseconds = values.Value.Attribute.TimeoutMilliseconds,
                InputAttributes = InputAttributes.Merge(_parameters),
            };

            var events = new RequestEvents
            {
                OnSuccess = (val, resp) => OnRequestSuccess(values.Value, val, resp),
                OnError = OnRequestError,
            };

            Task.Run(async () =>
            {
                var key = await FireHttpRequest(parameters, events);
                var state = new HttpValueState(values.Value.Property, values.Value.Attribute, values.Value.Key);
                _cache.TryUpdate(values.Key, state, values.Value);
            });
        }

        return Task.CompletedTask;
    }

    private Task OnRequestError(HttpValueErrorState errorState, Exception? exception, HttpResponseMessage? response)
    {
        return Task.CompletedTask;
    }

    private Task OnRequestSuccess(HttpValueState state, object? val, HttpResponseMessage resp)
    {
        return InvokeAsync(() =>
        {
            state.Property.SetValue(this, val);
            Console.WriteLine("Setting {0}={1}", state.Property.Name, val);
            StateHasChanged();
        });
    }
}
