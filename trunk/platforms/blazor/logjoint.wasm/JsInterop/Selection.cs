using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class SelectionInterop
    {
        readonly IJSRuntime jsRuntime;

        public SelectionInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async ValueTask<string> GetSelectionInElement(ElementReference element)
        {
            return await jsRuntime.InvokeAsync<string>("logjoint.selection.getSelectedTextInElement", element);
        }
    }
}
