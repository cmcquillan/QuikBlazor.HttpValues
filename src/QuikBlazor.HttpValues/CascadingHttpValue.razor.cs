using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikBlazor.HttpValues;

public partial class CascadingHttpValue<TValue> : HttpValueBase<TValue>
{
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    [Parameter]
    public RenderFragment<TValue?>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? ErrorTemplate { get; set; }

    protected bool IsLoading { get; set; }

    protected override void OnInitialized()
    {
        IsLoading = true;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await FireHttpRequest();
    }

    protected override Task OnNewValueAsync(TValue? value)
    {
        IsLoading = false;

        return Task.CompletedTask;
    }
}
