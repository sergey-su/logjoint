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

		event EventHandler<SettingsChangeEvent> Changed;
	}

	[Flags]
	public enum SettingsPiece
	{
		None = 0,
		FileSizes = 1,
		Appearance = 2
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

	public struct Appearance
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
		public LogFontSize FontSize;

		/// <summary>
		/// Font family name or null if platform-default is to be used
		/// </summary>
		public string FontFamily;

		public enum ColoringMode
		{
			None,
			Threads,
			Sources,
			Minimum = None,
			Maximum = Sources
		};
		public ColoringMode Coloring;

		public PaletteBrightness ColoringBrightness;

		static public readonly Appearance Default = new Appearance()
		{
			FontSize = LogFontSize.Normal,
			Coloring = ColoringMode.Threads,
			ColoringBrightness = PaletteBrightness.Increased
		};
	};
}
