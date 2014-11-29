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

		public byte A { get { unchecked { return (byte)(argb >> 24); } } }
		public byte R { get { unchecked { return (byte)(argb >> 16); } } }
		public byte G { get { unchecked { return (byte)(argb >> 8); } } }
		public byte B { get { unchecked { return (byte)(argb); } } }

		public uint Argb { get { return argb; } }

		public ModelColor MakeDarker(byte delta)
		{
			return new ModelColor(A, Dec(R, delta), Dec(G, delta), Dec(B, delta));
		}

		public ModelColor MakeLighter(byte delta)
		{
			return new ModelColor(A, Inc(R, delta), Inc(G, delta), Inc(B, delta));
		}

#if !SILVERLIGHT
		public Color ToColor() 
		{
			unchecked
			{
				return Color.FromArgb((int)argb);
			}
		}
#endif
		static byte Dec(byte v, byte delta)
		{
			if (v <= delta)
				return 0;
			return (byte)(v - delta);
		}

		static byte Inc(byte v, byte delta)
		{
			if (0xff - v <= delta)
				return 0xff;
			return (byte)(v + delta);
		}

		uint argb;
	}
}
