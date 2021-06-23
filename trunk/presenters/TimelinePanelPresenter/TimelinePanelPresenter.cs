
using System;

namespace LogJoint.UI.Presenters.TimelinePanel
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly Timeline.IPresenter timelinePresenter;
		readonly IChangeNotification changeNotification;
		bool isVisible = true;
		double? size = null;

		public Presenter(
			IView view,
			IChangeNotification changeNotification,
			Timeline.IPresenter timelinePresenter)
		{
			this.timelinePresenter = timelinePresenter;
			this.changeNotification = changeNotification;

			view.SetViewModel(this);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		bool IViewModel.IsEnabled => !timelinePresenter.IsEmpty;

		bool IViewModel.IsVisible => isVisible;

		double? IViewModel.Size => size;

		string IViewModel.HideButtonTooltip => "Hide timeline";

		string IViewModel.ShowButtonTooltip => "Show timeline";

		string IViewModel.ResizerTooltip => "Resize timeline";

		void IViewModel.OnZoomToolButtonClicked(int delta)
		{
			timelinePresenter.Zoom(delta);
		}

		void IViewModel.OnZoomToViewAllToolButtonClicked()
		{
			timelinePresenter.ZoomToViewAll();
		}

		void IViewModel.OnScrollToolButtonClicked(int delta)
		{
			timelinePresenter.Scroll(delta);
		}

		void IViewModel.OnResize(double size)
		{
			if (isVisible)
			{
				this.size = Math.Max(0, size);
				changeNotification.Post();
			}
		}

		void IViewModel.OnHideButtonClicked()
		{
			if (isVisible)
			{
				isVisible = false;
				changeNotification.Post();
			}
		}

		void IViewModel.OnShowButtonClicked()
		{
			if (!isVisible)
			{
				isVisible = true;
				changeNotification.Post();
			}
		}
	}
};