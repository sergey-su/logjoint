using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class FormsInterop
    {
        readonly IJSRuntime jsRuntime;

        public FormsInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        // Sets the value of <select> to the same value as it has now.
        // This triggers an update of <selectedcontent> that is otherwise does not "see"
        // the dynamic options' selection by blazor code.
        public ValueTask ResetSelectValue(ElementReference element)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.forms.resetSelectValue", element);
        }
    }
}
