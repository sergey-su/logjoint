using System.Drawing;

namespace LogJoint.Drawing
{
	partial class Brush
	{
		internal Color color;

		partial void Init(Color color)
		{
			this.color = color;
		}

		public void Dispose()
		{
		}
	};
}