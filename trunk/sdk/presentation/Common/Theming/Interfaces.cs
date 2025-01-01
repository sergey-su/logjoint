using LogJoint.Drawing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters
{
    public interface IColorTheme
    {
        ColorThemeMode Mode { get; }
        ImmutableArray<Color> ThreadColors { get; }
        ImmutableArray<Color> HighlightingColors { get; }
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
        ImmutableArray<Color> Items { get; }
    };
}