using System.Drawing;
using System.Drawing.Drawing2D;

namespace LogJoint.Drawing
{
	public partial class Pen
	{
		internal System.Drawing.Pen pen;

		public System.Drawing.Pen NativePen { get { return pen; } }

		partial void Init(Color color, float width, float[] dashPattern)
		{
			this.pen = new System.Drawing.Pen(color, width);
			if (dashPattern != null)
			{
				this.pen.DashStyle = DashStyle.Custom;
				this.pen.DashPattern = dashPattern;
			}
		}
	};
}