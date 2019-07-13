using System;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
	class PermissiveColoringModeStrategy : IColoringModeStrategy
	{
		readonly IChangeNotification changeNotification;
		Appearance.ColoringMode coloringMode = Appearance.ColoringMode.Threads;

		public PermissiveColoringModeStrategy(
			IChangeNotification changeNotification
		)
		{
			this.changeNotification = changeNotification;
		}

		Appearance.ColoringMode IColoringModeStrategy.Coloring
		{
			get => coloringMode;
			set
			{
				if (value != coloringMode)
				{
					coloringMode = value;
					changeNotification.Post();
				}
			}
		}

		void IDisposable.Dispose()
		{
		}
	};
};
