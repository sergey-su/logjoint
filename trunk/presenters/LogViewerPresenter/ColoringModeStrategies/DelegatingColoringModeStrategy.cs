using System;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.LogViewer
{
	class DelegatingColoringModeStrategy : IColoringModeStrategy
	{
		readonly IPresenterInternal referencePresenter;

		public DelegatingColoringModeStrategy(
			IPresenterInternal referencePresenter
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
