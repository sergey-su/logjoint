using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.TimelinePanel
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			ILogSourcesManager logSources,
			IBookmarks bookmarks,
			IView view,
			Presenters.Timeline.IPresenter timelinePresenter,
			IHeartBeatTimer heartbeat)
		{
			this.logSources = logSources;
			this.bookmarks = bookmarks;
			this.view = view;
			this.timelinePresenter = timelinePresenter;

			this.logSources.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.CachedTime | LogProviderStatsFlag.AvailableTime)) != 0)
					lazyUpdateFlag.Invalidate();
			};
			this.logSources.OnLogTimeGapsChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.logSources.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.logSources.OnLogSourceRemoved += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && lazyUpdateFlag.Validate())
					UpdateView();
			};

			view.SetPresenter(this);
			view.SetEnabled(false);
		}


		void IViewEvents.OnZoomToolButtonClicked(int delta)
		{
			timelinePresenter.Zoom(delta);
		}

		void IViewEvents.OnZoomToViewAllToolButtonClicked()
		{
			timelinePresenter.ZoomToViewAll();
		}

		void IViewEvents.OnScrollToolButtonClicked(int delta)
		{
			timelinePresenter.Scroll(delta);
		}

		#region Implementation

		void UpdateView()
		{
			timelinePresenter.UpdateView();
			view.SetEnabled(!timelinePresenter.IsEmpty);
		}

		readonly ILogSourcesManager logSources;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly Presenters.Timeline.IPresenter timelinePresenter;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();

		#endregion
	};
};