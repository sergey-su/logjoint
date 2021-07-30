using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class ChromeExtensionInterop
    {
        readonly IJSRuntime jsRuntime;
        DotNetObjectReference<CallbackHelper> callbackObject;

        public class OpenLogEventArgs: EventArgs
        {
            public string Id { get; private set; }
            public string DisplayName { get; private set; }
            public OpenLogEventArgs(string id, string displayName)
            {
                Id = id;
                DisplayName = displayName;
            }
        }

        public ChromeExtensionInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public ValueTask Init()
        {
            callbackObject = DotNetObjectReference.Create(new CallbackHelper() { owner = this });
            return jsRuntime.InvokeVoidAsync("logjoint.chrome_extension.init", callbackObject);
        }

        public event EventHandler<OpenLogEventArgs> OnOpen;

        class CallbackHelper
        {
            public ChromeExtensionInterop owner;

            [JSInvokable]
            public void Open(string id, string displayName)
            {
                owner.OnOpen?.Invoke(owner, new OpenLogEventArgs(id, displayName));
            }
        }
    }
}
