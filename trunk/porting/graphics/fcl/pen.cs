using System.Drawing;

namespace LogJoint.Drawing
{
	public partial class Pen
	{
		internal System.Drawing.Pen pen;

		partial void Init(Color color, float width)
		{
			this.pen = new System.Drawing.Pen(color, width);
		}
	};
}