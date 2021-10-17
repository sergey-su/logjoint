using Microsoft.JSInterop;
using System.Threading.Tasks;

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
			Selection = new SelectionInterop(jsRuntime);
			Browser = new BrowserInterop(jsRuntime);
			Layout = new LayoutInterop(jsRuntime);
			Style = new Style(jsRuntime);
			ChromeExtension = new ChromeExtensionInterop(jsRuntime);
			IndexedDB = new IndexedDB(jsRuntime);
		}

		public async Task Init()
		{
			await Browser.Init();
			await ChromeExtension.Init();
		}

		public ResizeInterop Resize { get; private set; }
		public SaveAsInterop SaveAs { get; private set; }
		public KeyboardInterop Keyboard { get; private set; }
		public ScrollInterop Scroll { get; private set; }
		public MouseInterop Mouse { get; private set; }
		public SelectionInterop Selection { get; private set; }
		public BrowserInterop Browser { get; private set; }
		public LayoutInterop Layout { get; private set; }
		public Style Style { get; private set; }
		public ChromeExtensionInterop ChromeExtension { get; private set; }
		public IndexedDB IndexedDB { get; private set; }
	}
}
