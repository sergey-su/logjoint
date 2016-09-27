using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.BookmarksManager
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IBookmarks bookmarks,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			BookmarksList.IPresenter listPresenter,
			StatusReports.IPresenter statusReportFactory,
			IPresentersFacade navHandler,
			IViewUpdates viewUpdates,
			IAlertPopup alerts)
		{
			this.bookmarks = bookmarks;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.tracer = new LJTraceSource("UI", "ui.bmkm");
			this.statusReportFactory = statusReportFactory;
			this.searchResultPresenter = searchResultPresenter;
			this.viewUpdates = viewUpdates;
			this.navHandler = navHandler;
			this.listPresenter = listPresenter;
			this.alerts = alerts;

			viewerPresenter.FocusedMessageBookmarkChanged += delegate(object sender, EventArgs args)
			{
				listPresenter.SetMasterFocusedMessage(viewerPresenter.GetFocusedMessageBookmark());
			};
			listPresenter.Click += (s, bmk) =>
			{
				IPresenter myPublicIntf = this;
				myPublicIntf.NavigateToBookmark(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
			};

			view.SetPresenter(this);
		}

		void IPresenter.ShowNextBookmark()
		{
			NextBookmark(true);
		}

		void IPresenter.ShowPrevBookmark()
		{
			NextBookmark(false);
		}

		async Task<bool> IPresenter.NavigateToBookmark(IBookmark bmk, BookmarkNavigationOptions options)
		{
			var ret = await viewerPresenter.SelectMessageAt(bmk);
			if (!ret)
				HandleNavigateToBookmarkFailure(bmk, options);
			return ret;
		}

		void IPresenter.ToggleBookmark()
		{
			DoBookmarkAction(null);
		}

		void IViewEvents.OnToggleButtonClicked()
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Toggle Bookmark.");
				DoBookmarkAction(null);
			}
		}

		void IViewEvents.OnAddBookmarkButtonClicked()
		{
			DoBookmarkAction(true);
		}

		void IViewEvents.OnDeleteBookmarkButtonClicked()
		{
			listPresenter.DeleteSelectedBookmarks();
		}

		void IViewEvents.OnDeleteAllButtonClicked()
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Clear Bookmarks.");

				if (bookmarks.Count == 0)
				{
					tracer.Info("Nothing to clear");
					return;
				}

				if (alerts.ShowPopup(
					"Delete bookmarks",
					string.Format("Are you sure you to delete {0} bookmarks", bookmarks.Count),
					AlertFlags.YesNoCancel | AlertFlags.WarningIcon
				) != AlertFlags.Yes)
				{
					tracer.Info("User didn't confirm the cleaning");
					return;
				}

				bookmarks.Clear();

				UpdateOverallView();
			}
		}

		void IViewEvents.OnPrevBmkButtonClicked()
		{
			tracer.Info("----> User Command: Prev Bookmark.");
			NextBookmark(false);
		}

		void IViewEvents.OnNextBmkButtonClicked()
		{
			tracer.Info("----> User Command: Next Bookmark.");
			NextBookmark(true);
		}

		#region Implementation

		void UpdateOverallView()
		{
			viewUpdates.RequestUpdate();
		}

		void NextBookmark(bool forward)
		{
			NextBookmarkInternal(forward).IgnoreCancellation();
		}

		async Task NextBookmarkInternal(bool forward)
		{
			var firstBmk = bookmarks.GetNext(viewerPresenter.GetFocusedMessageBookmark(), forward);
			if (firstBmk == null)
			{
				statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Bookmarks",
					forward ? "Next bookmark not found" : "Prev bookmark not found", true);
			}
			else
			{
				if (!await viewerPresenter.SelectMessageAt(firstBmk))
				{
					bool reportFailure = true;
					var bookmarks = this.bookmarks.Items;
					if (!forward)
						bookmarks = bookmarks.Reverse();
					foreach (var followingBmk in bookmarks.SkipWhile(b => b != firstBmk).Skip(1))
					{
						if (await viewerPresenter.SelectMessageAt(followingBmk))
						{
							reportFailure = false;
							break;
						}
					}
					if (reportFailure)
					{
						HandleNavigateToBookmarkFailure(firstBmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
					}
				}
			}
		}

		void HandleNavigateToBookmarkFailure(IBookmark bmk, BookmarkNavigationOptions options)
		{
			if ((options & BookmarkNavigationOptions.EnablePopups) == 0)
				return;

			string popupCaption;
			string messageDescription;
			if ((options & BookmarkNavigationOptions.BookmarksStringsSet) != 0)
			{
				popupCaption = "Bookmarks";
				messageDescription = "Bookmarked message";
			}
			else if ((options & BookmarkNavigationOptions.SearchResultStringsSet) != 0)
			{
				popupCaption = "Search result";
				messageDescription = "Message";
			}
			else
			{
				popupCaption = "Warning";
				messageDescription = "Message";
			}

			bool noLinks = (options & BookmarkNavigationOptions.NoLinksInPopups) != 0;

			statusReportFactory.CreateNewStatusReport().ShowStatusPopup(popupCaption, messageDescription + " can not be shown", true);
		}

		private void DoBookmarkAction(bool? targetState)
		{
			IBookmark l = (searchResultPresenter != null && searchResultPresenter.LogViewerPresenter.HasInputFocus) ? 
				searchResultPresenter.GetFocusedMessageBookmark() : viewerPresenter.GetFocusedMessageBookmark();
			if (l == null)
				return;
			var bmks = bookmarks;
			if (targetState != null)
			{
				var pos = bmks.FindBookmark(l);
				if (targetState.Value == (pos.Item1 != pos.Item2))
					return;
			}
			bmks.ToggleBookmark(l);
			UpdateOverallView();
		}

		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly LJTraceSource tracer;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly SearchResult.IPresenter searchResultPresenter;
		readonly StatusReports.IPresenter statusReportFactory;
		readonly IPresentersFacade navHandler;
		readonly IViewUpdates viewUpdates;
		readonly BookmarksList.IPresenter listPresenter;
		readonly IAlertPopup alerts;

		#endregion
	};
};