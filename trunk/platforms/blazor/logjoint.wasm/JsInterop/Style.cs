using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LogJoint.Wasm
{
    public class Style
    {
        readonly IJSRuntime jsRuntime;

        public Style(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public string GetComputedStyle(ElementReference e, string property)
        {
            return ((IJSInProcessRuntime)jsRuntime).Invoke<string>("logjoint.style.getComputedStyle", e, property);
        }

        public void SetProperty(ElementReference e, string property, string value)
        {
            ((IJSInProcessRuntime)jsRuntime).InvokeVoid("logjoint.style.setProperty", e, property, value);
        }
    }
}
