using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace LogJoint
{
	abstract class ColorsTableBase
	{
		protected ColorsTableBase()
		{
			colors = GetColors();
		}

		public Color GenerateNewColor()
		{
			int idx = System.Threading.Interlocked.Increment(ref colorIndex);
			int color;
			unchecked
			{
				color = (int)0xff000000 | colors[(uint)idx % colors.Length];
			};
			return Color.FromArgb(color);
		}
		public void Reset()
		{
			colorIndex = -1;
		}

		static byte Dec(byte v, byte delta)
		{
			if (v <= delta)
				return 0;
			return (byte)(v - delta);
		}

		public static Color MakeDarker(Color cl, byte delta)
		{
			return Color.FromArgb(255, Dec(cl.R, delta), Dec(cl.G, delta), Dec(cl.B, delta));
		}
		public static Color MakeDarker(Color cl)
		{
			return MakeDarker(cl, 16);
		}

		protected abstract int[] GetColors();

		readonly int[] colors;
		int colorIndex = -1;
	}

	class PastelColorsGenerator : ColorsTableBase
	{
		protected override int[] GetColors()
		{
			return pastelColors;
		}

		static readonly int[] pastelColors = { 
			/*0xFFFFFF, 0xEEEEEE, 0xDDDDDD
			, 0xEEFFFF, 0xFFEEFF, 0xFFFFEE
			, 0xEEEEFF, 0xEEFFEE, 0xFFEEEE
			, */0xDDEEEE, 0xEEDDEE, 0xEEEEDD
			, 0xDDDDEE, 0xDDEEDD, 0xEEDDDD
			, 0xDDFFFF, 0xFFDDFF, 0xFFFFDD
			, 0xDDDDFF, 0xDDFFDD, 0xFFDDDD
			, 0xCCDDDD, 0xDDCCDD, 0xDDDDCC
			, 0xCCCCDD, 0xCCDDCC, 0xDDCCCC
			, 0xCCEEEE, 0xEECCEE, 0xEEEECC
			, 0xCCCCEE, 0xCCEECC, 0xEECCCC
			, 0xCCFFFF, 0xFFCCFF, 0xFFFFCC
			, 0xCCCCFF, 0xCCFFCC, 0xFFCCCC
		};
	}

	class HTMLColorsGenerator : ColorsTableBase
	{
		protected override int[] GetColors()
		{
			return htmlColors;
		}

		static readonly int[] htmlColors = { 
			0x7FFF00, 0xD2691E, 0xDC143C, 
			0x00FFFF, 0x7FFFD4, 0x8A2BE2,
			0xA52A2A, 0xDEB887, 0x5F9EA0, 
			0x006400, 0x8B008B, 0x556B2F, 
			0xFF8C00, 0x8B0000, 0x9400D3,
			0xFF1493, 0xFF0000, 0xFA8072,
			0xFFFF00, 0xFF6347
		};
	}

}
