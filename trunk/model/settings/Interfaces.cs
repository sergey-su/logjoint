using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.Settings
{
	public interface IGlobalSettingsAccessor
	{
		FileSizes FileSizes { get; set; }
		int MaxNumberOfHitsInSearchResultsView { get; set; }
		bool MultithreadedParsingDisabled { get; set; }
		Appearance Appearance { get; set; }
		StorageSizes UserDataStorageSizes { get; set; }
		StorageSizes ContentStorageSizes { get; set; }
		bool EnableAutoPostprocessing { get; set; }

		event EventHandler<SettingsChangeEvent> Changed;
	}

	[Flags]
	public enum SettingsPiece
	{
		None = 0,
		FileSizes = 1,
		Appearance = 2,
		UserDataStorageSizes = 4,
		ContentCacheStorageSizes = 8,
	};

	public class SettingsChangeEvent: EventArgs
	{
		public SettingsPiece ChangedPieces { get; private set; }

		public SettingsChangeEvent(SettingsPiece value)
		{
			ChangedPieces = value;
		}
	};

	public struct FileSizes
	{
		public int Threshold;
		public const int MaxThreshold = 200;
		
		public int WindowSize;
		public const int MinWindowSize = 1;
		public const int MaxWindowSize = 24;

		static public readonly FileSizes Default = new FileSizes() { Threshold = 30, WindowSize = 4 };
	};

	public class Appearance
	{
		public enum LogFontSize
		{
			SuperSmall = -3,
			ExtraSmall = -2,
			Small = -1,
			Normal = 0,
			Large1 = 1,
			Large2 = 2,
			Large3 = 3,
			Large4 = 4,
			Large5 = 5,
			Minimum = SuperSmall,
			Maximum = Large5
		};
		public LogFontSize FontSize { get; internal set; }

		/// <summary>
		/// Font family name or null if platform-default is to be used
		/// </summary>
		public string FontFamily { get; internal set; }

		public enum ColoringMode
		{
			None,
			Threads,
			Sources,
			Minimum = None,
			Maximum = Sources
		};
		public ColoringMode Coloring { get; internal set; }

		public PaletteBrightness ColoringBrightness { get; internal set; }

		public Appearance(LogFontSize fontSize, string fontFamily, ColoringMode coloring, PaletteBrightness coloringBrightness)
		{
			this.FontSize = fontSize;
			this.FontFamily = fontFamily;
			this.Coloring = coloring;
			this.ColoringBrightness = coloringBrightness;
		}

		static public readonly Appearance Default = new Appearance(LogFontSize.Normal, null, ColoringMode.Threads, PaletteBrightness.Normal);
	};

	public struct StorageSizes
	{
		public int StoreSizeLimit; // megs
		public const int MinStoreSizeLimit = 16;

		public int CleanupPeriod; // hours
		public const int MinCleanupPeriod = 8;
		public const int MaxCleanupPeriod = 24 * 7;

		static public readonly StorageSizes Default = new StorageSizes()
		{
			StoreSizeLimit = 300,
			CleanupPeriod = 24 * 3
		};
	};

	public enum PaletteBrightness
	{
		Decreased,
		Normal,
		Increased,
		Minimum = Decreased,
		Maximum = Increased
	};
}
