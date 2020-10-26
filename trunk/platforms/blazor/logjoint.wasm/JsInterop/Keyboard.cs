using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class KeyboardInterop
    {
        readonly IJSRuntime jsRuntime;

        public KeyboardInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Adds a keydown handler prevents default action for specified keys.
        /// The keys are specified in the format of https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key.
        /// Additionally a key can contains modifiers, for example, "Ctrl+F", "Meta+C".
        /// That's an alternative for the inflexible all-or-nothing @keydown:preventDefault mean.
        /// </summary>
        public ValueTask AddDefaultPreventingHandler(ElementReference element, params string[] keys)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.keyboard.addDefaultPreventingHandler", element, keys);
        }

        public ValueTask<bool> IsFocusWithin(ElementReference element)
        {
            return jsRuntime.InvokeAsync<bool>("logjoint.isFocusWithin", element);
        }
    }
}
