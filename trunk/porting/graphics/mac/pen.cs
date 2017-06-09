using System.Drawing;
using System.Linq;
using System;

namespace LogJoint.Drawing
{
	public partial class Pen
	{
		internal Color color;
		internal float width;
		internal nfloat[] dashPattern;

		partial void Init(Color color, float width, float[] dashPattern)
		{
			this.color = color;
			this.width = width;
			if (dashPattern != null)
			{
				var w = width != 0 ? width : 1;
				this.dashPattern = dashPattern.Select(x => (nfloat)(x * w)).ToArray();
			}
			else
			{
				this.dashPattern = null;
			}
		}
	}
}