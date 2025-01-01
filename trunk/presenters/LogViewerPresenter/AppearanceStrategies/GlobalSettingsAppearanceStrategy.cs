using System;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
    class GlobalSettingsAppearanceStrategy : IAppearanceStrategy
    {
        readonly IGlobalSettingsAccessor settings;
        readonly Func<FontData> font;

        public GlobalSettingsAppearanceStrategy(
            IGlobalSettingsAccessor settings
        )
        {
            this.settings = settings;
            this.font = Selectors.Create(() => settings.Appearance, a => new FontData(a.FontFamily, a.FontSize));
        }

        Appearance.ColoringMode IAppearanceStrategy.Coloring => settings.Appearance.Coloring;

        void IAppearanceStrategy.SetColoring(Appearance.ColoringMode value)
        {
            if (value != settings.Appearance.Coloring)
            {
                var a = settings.Appearance;
                settings.Appearance = new Appearance(a.FontSize, a.FontFamily, value, a.ColoringBrightness, a.Theme);
            }
        }

        FontData IAppearanceStrategy.Font => font();

        void IAppearanceStrategy.SetFont(FontData value)
        {
            if (value.Size != font().Size || value.Name != font().Name)
            {
                var a = settings.Appearance;
                settings.Appearance = new Appearance(value.Size, value.Name, a.Coloring, a.ColoringBrightness, a.Theme);
            }
        }
    };
};
