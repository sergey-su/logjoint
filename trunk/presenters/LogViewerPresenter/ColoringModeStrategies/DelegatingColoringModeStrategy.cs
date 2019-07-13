using System;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
	class DelegatingColoringModeStrategy : IColoringModeStrategy
	{
		readonly IPresenter referencePresenter;

		public DelegatingColoringModeStrategy(
			IPresenter referencePresenter
		)
		{
			this.referencePresenter = referencePresenter;
		}

		Appearance.ColoringMode IColoringModeStrategy.Coloring
		{
			get => referencePresenter.Coloring;
			set => referencePresenter.Coloring = value;
		}

		void IDisposable.Dispose()
		{
		}
	};
};
