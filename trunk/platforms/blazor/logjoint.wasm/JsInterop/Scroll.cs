using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class ScrollInterop
    {
        readonly IJSRuntime jsRuntime;

        public ScrollInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public ValueTask ScrollIntoView(ElementReference element)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.scroll.scrollIntoView", element);
        }

        public void ScrollLeftIntoView(ElementReference element, double targetX)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoidAsync("logjoint.scroll.scrollLeftIntoView", element, targetX);
        }
    }
}
