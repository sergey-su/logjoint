using System.Drawing;

namespace LogJoint.Drawing
{
	partial class Brush
	{
		internal System.Drawing.Brush b;

		partial void Init(Color color)
		{
			b = new System.Drawing.SolidBrush(color);
		}
			
		public void Dispose()
		{
			b.Dispose();
		}
	};
}