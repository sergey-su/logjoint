using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class MouseInterop
    {
        readonly IJSRuntime jsRuntime;

        public MouseInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public ValueTask SetMouseCapturingHandler(ElementReference element)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.mouse.setMouseCapturingHandler", element);
        }
    }
}
