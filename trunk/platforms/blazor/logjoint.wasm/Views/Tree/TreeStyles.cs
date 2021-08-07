using Microsoft.JSInterop;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Wasm.UI
{
	public class TreeStyles
	{
		private IJSRuntime jsRuntime;
		private int maxLevelStyleGenerated = -1;

		public TreeStyles(IJSRuntime jsRuntime)
		{
			this.jsRuntime = jsRuntime;
		}

		public async ValueTask EnsureNodeStyleExists(int level)
		{
			if (level > maxLevelStyleGenerated)
			{
				var styleBuilder = new StringBuilder();
				var stylesToGenerate = 10;
				for (int i = 0; i < stylesToGenerate; ++i)
				{
					int l = maxLevelStyleGenerated + 1 + i;
					styleBuilder.AppendLine($".tree>.node.c{l} {{ padding-left: {10*l}px; }}");
				}
				maxLevelStyleGenerated += stylesToGenerate;
				await jsRuntime.InvokeVoidAsync("logjoint.style.adoptStyle", styleBuilder.ToString());
			}
		}
	}
}
