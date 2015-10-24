using System.Drawing;

namespace LogJoint.Drawing
{
	public partial class Font
	{
		internal System.Drawing.Font font;

		public void Dispose()
		{
			font.Dispose();
		}

		partial void Init(string familyName, float emSize, FontStyle style)
		{
			this.font = new System.Drawing.Font(familyName, emSize, style);
		}
	};
}