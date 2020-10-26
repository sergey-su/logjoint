using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class ResizeInterop
    {
        readonly IJSRuntime jsRuntime;

        public ResizeInterop(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public enum Direction
        {
            Horizonal,
            Vertical
        };

        public async ValueTask<IDisposable> AddResizeHandler(
            ElementReference resizerElement,
            ElementReference targetElement,
            Direction direction,
            bool inverse,
            bool relativeToParent,
            Action<double> handler
        )
        {
            var resizeInvokeHelper = DotNetObjectReference.Create(new ResizeInvokeHelper { action = handler });
            await jsRuntime.InvokeVoidAsync(
                direction == Direction.Horizonal ? "logjoint.resize.initEWResizer" : "logjoint.resize.initNSResizer",
                resizerElement, targetElement, inverse, relativeToParent, handler != null ? resizeInvokeHelper : null);
            return resizeInvokeHelper;
        }

        class ResizeInvokeHelper
        {
            public Action<double> action;

            [JSInvokable]
            public void Invoke(double value) => action.Invoke(value);
        }
    }
}
