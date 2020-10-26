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

        public async ValueTask<IDisposable> InitResizer(
            ElementReference resizerElement,
            ElementReference targetElement,
            Direction direction,
            bool inverse,
            bool relativeToParent,
            Action<double> handler
        )
        {
            var resizeInvokeHelper = DotNetObjectReference.Create(new ResizerHelper { action = handler });
            await jsRuntime.InvokeVoidAsync(
                direction == Direction.Horizonal ? "logjoint.resize.initEWResizer" : "logjoint.resize.initNSResizer",
                resizerElement, targetElement, inverse, relativeToParent, handler != null ? resizeInvokeHelper : null);
            return resizeInvokeHelper;
        }

        public async ValueTask<IAsyncDisposable> ObserveResize(ElementReference element, Action handler)
        {
            var helperRef = DotNetObjectReference.Create(new ResizeObserverHelper { action = handler });
            var jsHandle = await jsRuntime.InvokeAsync<long>("logjoint.resize.observe", element, helperRef);
            return new ResizeObserverHandle
            {
                jsRuntime = jsRuntime,
                jsHandle = jsHandle,
                helperRef = helperRef,
            };
        }

        class ResizerHelper
        {
            public Action<double> action;

            [JSInvokable]
            public void Invoke(double value) => action.Invoke(value);
        }

        class ResizeObserverHelper
        {
            public Action action;

            [JSInvokable]
            public void OnResize() => action.Invoke();
        }

        class ResizeObserverHandle : IAsyncDisposable
        {
            public IJSRuntime jsRuntime;
            public IDisposable helperRef;
            public long jsHandle;

            async ValueTask IAsyncDisposable.DisposeAsync()
            {
                helperRef.Dispose();
                await jsRuntime.InvokeVoidAsync("logjoint.resize.unobserve", jsHandle);
            }
        };
    }
}
