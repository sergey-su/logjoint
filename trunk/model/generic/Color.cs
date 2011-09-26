using System;
using System.Collections.Generic;
using System.Text;
#if !SILVERLIGHT
using System.Drawing;
#endif

namespace LogJoint
{
	public struct ModelColor
	{
		public ModelColor(uint argb) { this.argb = argb; }
		public ModelColor(byte alpha, byte red, byte green, byte blue)
		{
			argb = (uint)red << 16 | (uint)green << 8 | (uint)blue | (uint)alpha << 24;
		}

		public uint Argb { get { return argb; } }

#if !SILVERLIGHT
		public Color ToColor() 
		{
			unchecked
			{
				return Color.FromArgb((int)argb);
			}
		}
#endif

		uint argb;
	}
}
