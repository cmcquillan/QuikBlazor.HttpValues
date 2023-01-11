using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpBindings;

public class CascadingHttpValue<TValue> : CascadingHttpValueBase<TValue>
{
    protected override Task OnNewValueAsync(TValue? value)
    {
        return Task.CompletedTask;
    }
}
