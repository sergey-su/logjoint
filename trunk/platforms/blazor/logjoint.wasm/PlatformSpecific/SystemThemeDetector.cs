using LogJoint.UI.Presenters;

namespace LogJoint.Wasm
{
    class SystemThemeDetector : ISystemThemeDetector
    {
        ColorThemeMode ISystemThemeDetector.Mode => ColorThemeMode.Light;
    }
}
