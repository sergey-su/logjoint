using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
	public class JsInterop
	{
		public JsInterop(IJSRuntime jsRuntime)
		{
			Resize = new ResizeInterop(jsRuntime);
			SaveAs = new SaveAsInterop(jsRuntime);
		}

		public ResizeInterop Resize { get; private set; }
		public SaveAsInterop SaveAs { get; private set; }
	}
}
