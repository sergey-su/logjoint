using System;
using System.Drawing;

namespace LogJoint.Drawing
{
	partial class Brush
	{
		Func<Color> getter;
		Color value;

		internal Color color { get { return getter != null ? getter () : value; } }

		partial void Init(Color color)
		{
			value = color;
		}

		partial void Init(Func<Color> color)
		{
			getter = color;
		}

		public void Dispose()
		{
		}
	};
}