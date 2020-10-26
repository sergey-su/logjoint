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
		}

		public ResizeInterop Resize { get; private set; }
		public SaveAsInterop SaveAs { get; private set; }
		public KeyboardInterop Keyboard { get; private set; }
	}
}
