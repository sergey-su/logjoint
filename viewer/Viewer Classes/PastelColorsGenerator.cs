using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace LogJoint
{
	public abstract class ColorTableBase
	{
		protected ColorTableBase()
		{
			colors = GetColors();
			refCounters = new int[colors.Length];
		}

		public int Count
		{
			get { return colors.Length; }
		}

		public IEnumerable<Color> Items
		{
			get
			{ 
				foreach (int i in colors)
					yield return FromRGB(i); 
			}
		}

		public struct ColorTableEntry
		{
			public readonly int ID;
			public readonly Color Color;

			public ColorTableEntry(int id, Color cl)
			{
				ID = id;
				Color = cl;
			}
		};

		public ColorTableEntry GetNextColor(bool addRef)
		{
			int retIdx = 0;
			lock (sync)
			{
				int minRefcounter = int.MaxValue;
				for (int idx = 0; idx < colors.Length; ++idx)
				{
					int refCount = refCounters[idx];
					if (refCount < minRefcounter)
					{
						minRefcounter = refCounters[idx];
						retIdx = idx;
					}
				}
				if (addRef)
				{
					++refCounters[retIdx];
				}
			}
			return new ColorTableEntry(retIdx, FromRGB(colors[(uint)retIdx % colors.Length]));
		}
		public void AddRef(int id)
		{
			lock (sync)
			{
				++refCounters[id];
			}
		}
		public void ReleaseColor(int id)
		{
			lock (sync)
			{
				if (refCounters[id] > 0)
					--refCounters[id];
			}
		}
		public int? FindColor(Color color)
		{
			int ret = 0;
			foreach (int cl in colors)
			{
				if (color == FromRGB(cl))
					return ret;
				ret++;
			}
			return null;
		}

		public void Reset()
		{
			lock (sync)
			{
				for (int idx = 0; idx < refCounters.Length; ++idx)
					refCounters[idx] = 0;
			}
		}

		static Color FromRGB(int rgb)
		{
			int color;
			unchecked
			{
				color = (int)0xff000000 | rgb;
			};
			return Color.FromArgb(color);
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

		readonly object sync = new object();
		readonly int[] colors;
		readonly int[] refCounters;
	}

	class PastelColorsGenerator : ColorTableBase
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

	class HTMLColorsGenerator : ColorTableBase
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
