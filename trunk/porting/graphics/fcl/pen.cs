
namespace LogJoint.Drawing
{
	public partial class Pen
	{
		internal System.Drawing.Pen pen;

		public System.Drawing.Pen NativePen { get { return pen; } }

		partial void Init(Color color, float width, float[] dashPattern)
		{
			this.pen = new System.Drawing.Pen(color.ToSystemDrawingObject(), width);
			if (dashPattern != null)
			{
				this.pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
				this.pen.DashPattern = dashPattern;
			}
		}
	};
}