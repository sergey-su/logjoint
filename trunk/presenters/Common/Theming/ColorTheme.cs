using LogJoint.Drawing;
using System.Collections.Immutable;
using System;

namespace LogJoint.UI.Presenters
{
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
            this.highlightingColors = new HighlightBackgroundColorsTable(this);
        }

        ColorThemeMode IColorTheme.Mode => systemThemeDetector.Mode;

        ImmutableArray<Color> IColorTheme.ThreadColors => threadColors.Items;

        ImmutableArray<Color> IColorTheme.HighlightingColors => highlightingColors.Items;
    };

    public class StaticSystemThemeDetector : ISystemThemeDetector
    {
        public StaticSystemThemeDetector(ColorThemeMode mode)
        {
            Mode = mode;
        }

        public ColorThemeMode Mode { get; private set; }
    };

    public class GlobalSettingsSystemThemeDetector : ISystemThemeDetector
    {
        readonly Settings.IGlobalSettingsAccessor settings;
        readonly Func<ColorThemeMode> mode;

        public GlobalSettingsSystemThemeDetector(Settings.IGlobalSettingsAccessor settings)
        {
            this.settings = settings;
            this.mode = Selectors.Create(() => settings.Appearance,
                a => a.Theme == Settings.Appearance.ColorTheme.Light ? ColorThemeMode.Light : ColorThemeMode.Dark);
        }

        ColorThemeMode ISystemThemeDetector.Mode => mode();

        public ColorThemeMode Mode => mode();

        public void SetMode(ColorThemeMode mode)
        {
            var a = settings.Appearance;
            settings.Appearance = new Settings.Appearance(a.FontSize, a.FontFamily, a.Coloring, a.ColoringBrightness,
                mode == ColorThemeMode.Light ? Settings.Appearance.ColorTheme.Light : Settings.Appearance.ColorTheme.Dark);
        }
    };
}