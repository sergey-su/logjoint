using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace LogJoint.Wasm
{
    public class BrowserInterop
    {
        readonly IJSRuntime jsRuntime;
        bool isMac;

        public BrowserInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async Task Init()
        {
            isMac = await jsRuntime.InvokeAsync<bool>("logjoint.browser.isMac");
        }

        public bool IsMac => isMac;

        public bool HasEditKey(MouseEventArgs eventArgs)
        {
            return isMac ? eventArgs.MetaKey : eventArgs.CtrlKey;
        }
        public bool HasEditKey(KeyboardEventArgs eventArgs)
        {
            return isMac ? eventArgs.MetaKey : eventArgs.CtrlKey;
        }
    }
}
