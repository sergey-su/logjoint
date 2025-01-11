using System;

namespace LogJoint.Settings
{
    // Single-threaded. Should be accessed only from model threading context.
    public interface IGlobalSettingsAccessor
    {
        FileSizes FileSizes { get; set; }
        int MaxNumberOfHitsInSearchResultsView { get; set; }
        bool MultithreadedParsingDisabled { get; set; }
        Appearance Appearance { get; set; }
        StorageSizes UserDataStorageSizes { get; set; }
        StorageSizes ContentStorageSizes { get; set; }
        bool EnableAutoPostprocessing { get; set; }
    }

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
        public LogFontSize FontSize { get; private set; }

        /// <summary>
        /// Font family name or null if platform-default is to be used
        /// </summary>
        public string FontFamily { get; private set; }

        public enum ColoringMode
        {
            None,
            Threads,
            Sources,
            Minimum = None,
            Maximum = Sources
        };
        public ColoringMode Coloring { get; private set; }

        public PaletteBrightness ColoringBrightness { get; private set; }

        public enum ColorTheme
        {
            Light,
            Dark,
            Minimum = Light,
            Maximum = Dark,
        };
        public ColorTheme Theme { get; private set; }

        public Appearance(LogFontSize fontSize, string fontFamily, ColoringMode coloring, PaletteBrightness coloringBrightness, ColorTheme theme)
        {
            this.FontSize = fontSize;
            this.FontFamily = fontFamily;
            this.Coloring = coloring;
            this.ColoringBrightness = coloringBrightness;
            this.Theme = theme;
        }

        static public readonly Appearance Default = new Appearance(LogFontSize.Normal, null, ColoringMode.Threads, PaletteBrightness.Normal, ColorTheme.Light);
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
