using System.Drawing;
using MonoMac.CoreText;
using MonoMac.AppKit;

namespace LogJoint.Drawing
{
	partial class Font
	{
		internal NSFont font;
		internal FontStyle style;

		partial void Init(string familyName, float emSize, FontStyle style)
		{
			this.font = NSFont.FromFontName(familyName, emSize);
			this.style = style;
		}

		public void Dispose()
		{
			font.Dispose();
		}
	};
}