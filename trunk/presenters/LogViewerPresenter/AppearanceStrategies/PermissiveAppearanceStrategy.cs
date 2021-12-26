using System;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
	class PermissiveAppearanceStrategy : IAppearanceStrategy
	{
		readonly IChangeNotification changeNotification;
		Appearance.ColoringMode coloringMode = Appearance.ColoringMode.Threads;
		FontData font = new FontData();

		public PermissiveAppearanceStrategy(
			IChangeNotification changeNotification
		)
		{
			this.changeNotification = changeNotification;
		}

		Appearance.ColoringMode IAppearanceStrategy.Coloring => coloringMode;

		void IAppearanceStrategy.SetColoring(Appearance.ColoringMode value)
		{
			if (value != coloringMode)
			{
				coloringMode = value;
				changeNotification.Post();
			}
		}

		FontData IAppearanceStrategy.Font => font;

		void IAppearanceStrategy.SetFont(FontData value)
		{
			font = value;
			changeNotification.Post();
		}
	};
};
