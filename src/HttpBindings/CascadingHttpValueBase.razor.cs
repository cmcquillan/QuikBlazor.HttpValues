using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HttpBindings;

public abstract partial class CascadingHttpValueBase<TValue> : HttpValueBase<TValue>
{
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    [Parameter]
    public RenderFragment<TValue?>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? ErrorTemplate { get; set; }
}
