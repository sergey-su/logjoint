using System.Drawing;
using MonoMac.CoreText;

namespace LogJoint.Drawing
{
	partial class Font
	{
		internal CTFont font;

		partial void Init(string familyName, float emSize)
		{
			this.font = new CTFont(familyName, emSize);
		}

		public void Dispose()
		{
			font.Dispose();
		}
	};
}