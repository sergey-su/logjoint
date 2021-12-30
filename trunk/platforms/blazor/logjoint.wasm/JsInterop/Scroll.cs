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

        public double GetScrollTop(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.scroll.getScrollTop", element);
        }

        public double GetScrollLeft(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.scroll.getScrollLeft", element);
        }

        public void SetScrollTop(ElementReference element, double value)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("logjoint.scroll.setScrollTop", element, value);
        }
    }
}
