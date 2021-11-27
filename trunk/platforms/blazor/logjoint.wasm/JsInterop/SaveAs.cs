using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class SaveAsInterop
    {
        readonly IJSRuntime jsRuntime;

        public SaveAsInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public ValueTask SaveAs(string contents, string name)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.saveAs", contents, name);
        }
        public ValueTask SaveAs(byte[] contents, string name)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.saveAs", contents, name);
        }
    }
}
