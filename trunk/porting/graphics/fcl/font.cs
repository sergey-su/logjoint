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

		partial void Init(string familyName, float emSize)
		{
			this.font = new System.Drawing.Font(familyName, emSize);
		}
	};
}