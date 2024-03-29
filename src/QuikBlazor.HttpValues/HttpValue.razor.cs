﻿using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikBlazor.HttpValues;

public partial class HttpValue<TValue> : HttpValueBase<TValue>
{
    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    protected override async Task OnNewValueAsync(TValue? value)
    {
        await InvokeAsync(async () =>
        {
            await ValueChanged.InvokeAsync(value);
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await FireHttpRequest();
    }
}
