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
            public string LogText { get; private set; }
            public string Id { get; private set; }
            public string Url { get; private set; }
            public string DisplayName { get; private set; }
            public OpenLogEventArgs(string logText, string id, string url, string displayName)
            {
                LogText = logText;
                Id = id;
                Url = url;
                DisplayName = displayName;
            }
        }

        public class AddSourceEventArgs: EventArgs
        {
            public string Url { get; private set; }
            public AddSourceEventArgs(string url)
            {
                Url = url;
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
        public event EventHandler<AddSourceEventArgs> OnAddSource;

        class CallbackHelper
        {
            public ChromeExtensionInterop owner;

            [JSInvokable]
            public void Open(string logText, string id, string url, string displayName)
            {
                owner.OnOpen?.Invoke(owner, new OpenLogEventArgs(logText, id, url, displayName));
            }

            [JSInvokable]
            public void AddSource(string url)
            {
                owner.OnAddSource?.Invoke(owner, new AddSourceEventArgs(url));
            }
        }
    }
}
