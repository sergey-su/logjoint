using Microsoft.JSInterop;

namespace LogJoint.Wasm
{
	public class JsInterop
	{
		public JsInterop(IJSRuntime jsRuntime)
		{
			Resize = new ResizeInterop(jsRuntime);
			SaveAs = new SaveAsInterop(jsRuntime);
			Keyboard = new KeyboardInterop(jsRuntime);
			Scroll = new ScrollInterop(jsRuntime);
			Mouse = new MouseInterop(jsRuntime);
		}

		public ResizeInterop Resize { get; private set; }
		public SaveAsInterop SaveAs { get; private set; }
		public KeyboardInterop Keyboard { get; private set; }
		public ScrollInterop Scroll { get; private set; }
		public MouseInterop Mouse { get; private set; }
	}
}
