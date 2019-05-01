using System;
using System.Collections.Immutable;
using System.Linq;

namespace LogJoint
{
	public interface IColorTable // todo: consider moving to presentation layer
	{
		ImmutableArray<ModelColor> Items { get; } // todo: make it a part of theme?
	};

	public static class ColorTableExtensions // todo: move to separate file
	{
		public static ModelColor GetByIndex(this IColorTable table, int index)
		{
			return GetByIndex(table.Items, index);
		}

		public static ModelColor GetByIndex(this ImmutableArray<ModelColor> table, int index)
		{
			return table[index % table.Length];
		}

		public static ModelColor? GetByIndex(this ImmutableArray<ModelColor> table, int? index)
		{
			return index != null ? table[index.Value % table.Length] : new ModelColor?();
		}
	};

	public interface IColorLease
	{
		int GetNextColor(int? preferredColor = null);
		void ReleaseColor(int index);
		void Reset();
	};

	public interface IColorTheme
	{
		ColorThemeMode Mode { get; }
		ImmutableArray<ModelColor> ThreadColors { get; }
		ImmutableArray<ModelColor> HighlightingColors{ get; }
	};

	public interface ISystemThemeDetector
	{
		ColorThemeMode Mode { get; }
	};


	public enum PaletteBrightness
	{
		Decreased,
		Normal,
		Increased,
		Minimum = Decreased,
		Maximum = Increased
	};

	public enum ColorThemeMode
	{
		Light,
		Dark
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


	public class ColorLease : IColorLease
	{
		public ColorLease(int colorsCount)
		{
			this.refCounters = new int[colorsCount];
		}

		int IColorLease.GetNextColor(int? preferredColor)
		{
			int retIdx = 0;
			lock (sync)
			{
				int minRefcounter = int.MaxValue;
				for (int idx = 0; idx < refCounters.Length; ++idx)
				{
					if (preferredColor != null && preferredColor.Value == idx)
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
				++refCounters[retIdx];
				return retIdx;
			}
		}

		void IColorLease.ReleaseColor(int index)
		{
			lock (sync)
			{
				if (refCounters[index] > 0)
					--refCounters[index];
			}
		}

		void IColorLease.Reset()
		{
			lock (sync)
			{
				for (int idx = 0; idx < refCounters.Length; ++idx)
					refCounters[idx] = 0;
			}		
		}

		readonly object sync = new object();
		readonly int[] refCounters;
	};

	public class ColorTableBase : IColorTable // todo: delete this base class
	{
		protected void Init(Func<int[]> colorsSelector)
		{
			Init(Selectors.Create(
				colorsSelector,
				colors => ImmutableArray.CreateRange(colors.Select(FromRGB))
			));
		}

		protected void Init(Func<ImmutableArray<ModelColor>> colorsSelector)
		{
			this.colorsSelector = colorsSelector;
		}

		ImmutableArray<ModelColor> IColorTable.Items => colorsSelector();

		protected static ModelColor FromRGB(int rgb)
		{
			uint color;
			unchecked
			{
				color = (uint)0xff000000 | (uint)rgb;
			};
			return new ModelColor(color);
		}

		Func<ImmutableArray<ModelColor>> colorsSelector;
	}

	static class HSLColorsGenerator
	{
		static int MakeColor(double h, double s, double l)
		{
			// https://www.rapidtables.com/convert/color/hsl-to-rgb.html
			h *= 360d;
			var C = (1d - Math.Abs(2d*l - 1d)) * s;
			var X = C * (1d - Math.Abs((h / 60d) % 2d - 1d));
			var m = l - C/2d;
			var (rp, gp, bp) =
				h < 60  ? (C, X, 0d) :
				h < 120 ? (X, C, 0d) :
				h < 180 ? (0d, C, X) :
				h < 240 ? (0d, X, C) :
				h < 300 ? (X, 0d, C) :
				(C, 0, X);
			return unchecked((int)(new ModelColor(
				0, 
				(byte) ((rp + m) * 255),
				(byte) ((gp + m) * 255),
				(byte) ((bp + m) * 255)
			)).Argb);
		}

		public static int[] Generate(int numHues, double saturation, double lightness)
		{
			var rnd = new Random(23848);
			return
				Enumerable.Range(0, numHues)
				.Select(hidx => (double)hidx / (double)numHues)
				.Select(h => (color: MakeColor(h, saturation, lightness), order: rnd.Next()))
				.OrderBy(x => x.order)
				.Select(x => x.color)
				.ToArray();
		}
	};

	public class LogThreadsColorsTable : ColorTableBase
	{
		public LogThreadsColorsTable(
			IColorTheme colorTheme,
			Func<PaletteBrightness> brightnessSelector)
		{
			Init(Selectors.Create(
				() => colorTheme.Mode,
				brightnessSelector,
				(theme, brightness) =>
				{
					if (theme == ColorThemeMode.Light)
						return ImmutableArray.CreateRange(lightThemeBaseColors.Select(FromRGB).Select(cl => AdjustLightColor(cl, brightness)));
					else
						return ImmutableArray.CreateRange(
							new[] { 0xdcdcdc }
							.Union(HSLColorsGenerator.Generate(numHues: 16, saturation: 0.45, lightness: 0.65 + GetLightnessCorrection(brightness)))
							.Select(FromRGB)
						);
				}
			));
		}

		static readonly int[] lightThemeBaseColors = { 
			  0xDDEEEE, 0xEEDDEE, 0xEEEEDD
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

		static ModelColor AdjustLightColor(ModelColor color, PaletteBrightness brightness)
		{
			switch (brightness)
			{
				case PaletteBrightness.Increased:
					return color.MakeLighter(22);
				case PaletteBrightness.Decreased:
					return color.MakeDarker(16);
				default:
					return color;
			}
		}

		static double GetLightnessCorrection(PaletteBrightness brightness)
		{
			switch (brightness)
			{
				case PaletteBrightness.Increased:
					return +0.1;
				case PaletteBrightness.Decreased:
					return -0.1;
				default:
					return 0;
			}
		}
	};

	public class HTMLColorsGenerator : ColorTableBase
	{
		public HTMLColorsGenerator() { Init(() => htmlColors); }

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

	public class ForegroundColorsGenerator : ColorTableBase // todo: better name
	{
		public ForegroundColorsGenerator() { Init(() => colors); }

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
		public HighlightBackgroundColorsGenerator(
			IColorTheme colorTheme
		)
		{
			var numColors = FilterAction.IncludeAndColorizeLast - FilterAction.IncludeAndColorizeFirst + 1;
			if (lightThemeColors.Length != numColors)
				throw new Exception("inconsistent constants");
			Init(Selectors.Create(
				() => colorTheme.Mode,
				theme => theme == ColorThemeMode.Light ?
					lightThemeColors
				  : HSLColorsGenerator.Generate(numHues: numColors, saturation: 0.80, lightness: 0.30)
			));
		}

		static readonly int[] lightThemeColors = {
			0x00ffff,
			0x008000,
			0x87cefa,
			0x90ee90,
			0x4169e1,
			0x778899,
			0x7fffd4,
			0xffb6c1,
			0xffff00,
			0xf0e68c,
			0xff0000,
			0x9400d3,
			0xdda0dd,
			0xf08080,
			0xffa07a,
			0xffa500
		};
	};

	public class ColorTheme : IColorTheme
	{
		readonly ISystemThemeDetector systemThemeDetector;
		readonly IColorTable threadColors;
		readonly IColorTable highlightingColors;

		public ColorTheme(
			ISystemThemeDetector systemThemeDetector,
			LogJoint.Settings.IGlobalSettingsAccessor settings
		)
		{
			this.systemThemeDetector = systemThemeDetector;
			this.threadColors = new LogThreadsColorsTable(this, () => settings.Appearance.ColoringBrightness);
			this.highlightingColors = new HighlightBackgroundColorsGenerator(this);
		}

		ColorThemeMode IColorTheme.Mode => systemThemeDetector.Mode;

		ImmutableArray<ModelColor> IColorTheme.ThreadColors => threadColors.Items;

		ImmutableArray<ModelColor> IColorTheme.HighlightingColors => highlightingColors.Items;
	};

	public class StaticSystemThemeDetector : ISystemThemeDetector
	{
		public StaticSystemThemeDetector(ColorThemeMode mode)
		{
			Mode = mode;
		}

		public ColorThemeMode Mode { get; private set; }
	};
}
