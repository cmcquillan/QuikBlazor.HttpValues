using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace QuikBlazor.HttpValues;

public partial class HttpButton<TValue> : HttpValueBase<TValue>
{
    private readonly Dictionary<string, object> _buttonAttributes = new();

    [Parameter]
    public bool DisableOnWait { get; set; }

    [Parameter]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<HttpButtonEventArgs<TValue>> ResponseCallback { get; set; }

    protected async Task HandleClickEvent(MouseEventArgs mouseEvent)
    {
        if (DisableOnWait)
        {
            _buttonAttributes["disabled"] = true;
            StateHasChanged();
        }

        await FireHttpRequest(forceFire: true);
    }

    protected override async Task OnNewValueAsync(TValue? value)
    {
        try
        {
            if (ResponseCallback.HasDelegate)
            {
                await ResponseCallback.InvokeAsync(new HttpButtonEventArgs<TValue>(value, ErrorState));
            }
        }
        finally
        {
            _buttonAttributes.Remove("disabled");
        }
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        _buttonAttributes.Clear();

        foreach (var para in parameters)
        {
            if (para.Name.StartsWith("button-"))
            {
                _buttonAttributes.Add(para.Name.Replace("button-", string.Empty), para.Value);
            }
        }

        return base.SetParametersAsync(parameters);
    }
}
