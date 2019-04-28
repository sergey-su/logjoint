using System.Collections.Immutable;

namespace LogJoint.UI.Presenters
{
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

	public enum ColorThemeMode
	{
		Light,
		Dark
	};

	public interface IColorTable
	{
		ImmutableArray<ModelColor> Items { get; }
	};
}