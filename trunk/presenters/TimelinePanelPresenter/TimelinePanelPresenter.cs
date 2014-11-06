using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.TimelinePanel
{
	public class Presenter : IPresenter, IPresenterEvents
	{
		public Presenter(
			IModel model,
			IView view,
			Presenters.Timeline.IPresenter timelinePresenter,
			IHeartBeatTimer heartbeat)
		{
			this.model = model;
			this.view = view;
			this.timelinePresenter = timelinePresenter;

			this.model.SourcesManager.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.LoadedTime | LogProviderStatsFlag.AvailableTime)) != 0)
					lazyUpdateFlag.Invalidate();
			};
			this.model.SourcesManager.OnLogTimeGapsChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.model.SourcesManager.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.model.SourcesManager.OnViewTailModeChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.model.SourcesManager.OnLogSourceRemoved += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.model.Bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && lazyUpdateFlag.Validate())
					UpdateView();
			};

			view.SetPresenter(this);
		}


		void IPresenterEvents.OnZoomToolButtonClicked(int delta)
		{
			timelinePresenter.Zoom(delta);
		}

		void IPresenterEvents.OnZoomToViewAllToolButtonClicked()
		{
			timelinePresenter.ZoomToViewAll();
		}

		void IPresenterEvents.OnScrollToolButtonClicked(int delta)
		{
			timelinePresenter.Scroll(delta);
		}

		void IPresenterEvents.OnViewTailModeToolButtonClicked(bool viewTailModeRequested)
		{
			if (viewTailModeRequested)
				timelinePresenter.TrySwitchOnViewTailMode();
			else
				timelinePresenter.TrySwitchOffViewTailMode();
		}

		#region Implementation

		void UpdateView()
		{
			view.SetViewTailModeToolButtonState(model.SourcesManager.IsInViewTailMode);

			timelinePresenter.UpdateView();
		}

		readonly IModel model;
		readonly IView view;
		readonly Presenters.Timeline.IPresenter timelinePresenter;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();

		#endregion
	};
};