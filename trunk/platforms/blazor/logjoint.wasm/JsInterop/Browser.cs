using Microsoft.JSInterop;
using System.Threading.Tasks;

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
    }
}
