using System;

namespace LogJoint.Drawing
{
	partial class Brush
	{
		internal ColorRef color;

		partial void Init(ColorRef color)
		{
			this.color = color;
		}

		public void Dispose()
		{
		}
	};
}