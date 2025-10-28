using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Wasm
{
    public class KeyboardInterop
    {
        readonly IJSRuntime jsRuntime;
        bool isMac;

        public KeyboardInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async Task Init()
        {
            isMac = await jsRuntime.InvokeAsync<bool>("logjoint.browser.isMac");
            await jsRuntime.InvokeVoidAsync("logjoint.keyboard.init", isMac);
        }

        public struct Options
        {
            public Action Handler;
            public bool PreventDefault;
            public bool StopPropagation;
        };

        /// <summary>
        /// Adds a keydown handler for specified keys.
        /// The keys are specified in the format of https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key.
        /// Additionally a key can contains modifiers, for example, "Ctrl+F", "Meta+C".
        /// Each key can have options, for example, "Ctrl+F/i" for case-insensitive match for F key.
        /// That's an alternative for the inflexible all-or-nothing @keydown:preventDefault / @keydown:stopPropagation.
        /// </summary>
        public async ValueTask<IDisposable> AddHandler(ElementReference element, Options options, params string[] keys)
        {
            var resizeInvokeHelper = options.Handler != null ?
                DotNetObjectReference.Create(new Helper { action = options.Handler }) : null;
            await jsRuntime.InvokeVoidAsync("logjoint.keyboard.addHandler", element, keys,
                resizeInvokeHelper, options.PreventDefault, options.StopPropagation);
            return resizeInvokeHelper;
        }

        public async ValueTask AddDefaultPreventingHandler(ElementReference element, params string[] keys)
        {
            await AddHandler(element, new Options { PreventDefault = true }, keys);
        }

        public ValueTask<bool> IsFocusWithin(ElementReference element)
        {
            return jsRuntime.InvokeAsync<bool>("logjoint.focus.isFocusWithin", element);
        }

        public ValueTask<string> GetFocusedElementTag()
        {
            return jsRuntime.InvokeAsync<string>("logjoint.focus.getFocusedElementTag");
        }

        public async ValueTask<IAsyncDisposable> TrapFocusInModal(ElementReference modalElement)
        {
            var modal = await jsRuntime.InvokeAsync<IJSObjectReference>("logjoint.focus.trapFocusInModal", modalElement);
            return new ModalHandle
            {
                dispose = () => modal.InvokeVoidAsync("dispose")
            };
        }

        public bool HasEditKey(MouseEventArgs eventArgs)
        {
            // Duplicated the JS implemenation of logjoint.keyboard.hasEditKey
            return isMac ? eventArgs.MetaKey : eventArgs.CtrlKey;
        }

        public bool HasEditKey(KeyboardEventArgs eventArgs)
        {
            // Duplicated the JS implemenation of logjoint.keyboard.hasEditKey
            return isMac ? eventArgs.MetaKey : eventArgs.CtrlKey;
        }

        class Helper
        {
            public Action action;

            [JSInvokable]
            public void Invoke() => action.Invoke();
        }

        class ModalHandle : IAsyncDisposable
        {
            public Func<ValueTask> dispose;
            public ValueTask DisposeAsync() => dispose();
        };
    }
}
