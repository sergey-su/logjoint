using System.Drawing;

namespace LogJoint.Drawing
{
	public partial class Pen
	{
		internal Color color;
		internal float width;

		partial void Init(Color color, float width)
		{
			this.color = color;
			this.width = width;
		}
	}
}