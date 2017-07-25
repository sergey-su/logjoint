using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	public interface IColorTable
	{
		int Count { get; }
		IEnumerable<ModelColor> Items { get; }
		ColorTableEntry GetNextColor(bool addRef, ModelColor? preferredColor = null);
		void ReleaseColor(int id);
	};

	public struct ColorTableEntry
	{
		public readonly int ID;
		public readonly ModelColor Color;

		public ColorTableEntry(int id, ModelColor cl)
		{
			ID = id;
			Color = cl;
		}
	};

	public abstract class ColorTableBase : IColorTable
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

		public IEnumerable<ModelColor> Items
		{
			get
			{ 
				foreach (int i in colors)
					yield return FromRGB(i); 
			}
		}

		public ColorTableEntry GetNextColor(bool addRef, ModelColor? preferredColor)
		{
			int retIdx = 0;
			lock (sync)
			{
				int minRefcounter = int.MaxValue;
				for (int idx = 0; idx < colors.Length; ++idx)
				{
					if (preferredColor != null && preferredColor.Value.Argb == FromRGB(colors[idx]).Argb)
					{
						retIdx = idx;
						break;
					}
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
		public int? FindColor(ModelColor color)
		{
			int ret = 0;
			foreach (int cl in colors)
			{
				if (color.Argb == FromRGB(cl).Argb)
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

		static ModelColor FromRGB(int rgb)
		{
			uint color;
			unchecked
			{
				color = (uint)0xff000000 | (uint)rgb;
			};
			return new ModelColor(color);
		}

		protected abstract int[] GetColors();

		readonly object sync = new object();
		readonly int[] colors;
		readonly int[] refCounters;
	}

	public class PastelColorsGenerator : ColorTableBase
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

	public enum PaletteBrightness
	{
		Decreased,
		Normal,
		Increased,
		Minimum = Decreased,
		Maximum = Increased
	};

	public interface IAdjustingColorsGenerator : IColorTable
	{
		PaletteBrightness Brightness { get; set; }
	};

	public class AdjustingColorsGenerator : IAdjustingColorsGenerator, IColorTable
	{
		readonly IColorTable innerTable;
		PaletteBrightness paletteBrightness;

		public AdjustingColorsGenerator(IColorTable innerTable, PaletteBrightness paletteBrightness)
		{
			this.innerTable = innerTable;
			this.paletteBrightness = paletteBrightness;
		}

		PaletteBrightness IAdjustingColorsGenerator.Brightness { get { return paletteBrightness; } set { paletteBrightness = value; } }

		int IColorTable.Count
		{
			get { return innerTable.Count; }
		}

		IEnumerable<ModelColor> IColorTable.Items
		{
			get { return innerTable.Items.Select(AdjustColor); }
		}

		ColorTableEntry IColorTable.GetNextColor(bool addRef, ModelColor? preferredColor)
		{
			var tmp = innerTable.GetNextColor(addRef, preferredColor);
			return new ColorTableEntry(tmp.ID, AdjustColor(tmp.Color));
		}

		void IColorTable.ReleaseColor(int id)
		{
			innerTable.ReleaseColor(id);
		}

		ModelColor AdjustColor(ModelColor color)
		{
			switch (paletteBrightness)
			{
				case PaletteBrightness.Increased:
					return color.MakeLighter(22);
				case PaletteBrightness.Decreased:
					return color.MakeDarker(16);
				default:
					return color;
			}
		}
	};

	public class HTMLColorsGenerator : ColorTableBase
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

	public class ForegroundColorsGenerator : ColorTableBase
	{
		protected override int[] GetColors()
		{
			return colors;
		}

		static readonly int[] colors = {
			0x4E15406,
			0xC88D00,
			0xCC0000,
			0x204A87,
			0xFF0000,
			0xFFA500,
			0x008000,
			0x0000FF,
			0x4B0082,
			0xEE82EE,
		};
	};

	public class HighlightBackgroundColorsGenerator : ColorTableBase
	{
		protected override int[] GetColors()
		{
			return colors;
		}

		static readonly int[] colors = {
			0x00FFFF,
			0xFF00FF,
			0xFFFF00,
			0xFF0000,
			0x00FF00,
		};
	};
}
