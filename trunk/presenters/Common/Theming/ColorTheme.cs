using System.Collections.Immutable;

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