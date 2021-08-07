using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class LayoutInterop
    {
        readonly IJSRuntime jsRuntime;

        public LayoutInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public double GetElementWidth(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.layout.getElementWidth", element);
        }

        public double GetElementHeight(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.layout.getElementHeight", element);
        }

        public double GetElementOffsetLeft(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.layout.getElementOffsetLeft", element);
        }

        public double GetElementOffsetTop(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.layout.getElementOffsetTop", element);
        }

        public double GetElementScrollerHeight(ElementReference element)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<double>("logjoint.layout.getElementScrollerHeight", element);
        }
    }
}
