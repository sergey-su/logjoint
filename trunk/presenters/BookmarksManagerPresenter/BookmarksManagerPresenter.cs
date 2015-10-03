using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.BookmarksManager
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			BookmarksList.IPresenter listPresenter,
			LJTraceSource tracer,
			StatusReports.IPresenter statusReportFactory,
			IPresentersFacade navHandler,
			IViewUpdates viewUpdates)
		{
			this.model = model;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.listPresenter = listPresenter;
			this.tracer = tracer;
			this.statusReportFactory = statusReportFactory;
			this.searchResultPresenter = searchResultPresenter;
			this.viewUpdates = viewUpdates;
			this.navHandler = navHandler;

			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				listPresenter.SetMasterFocusedMessage(viewerPresenter.FocusedMessage);
			};
			listPresenter.Click += (s, bmk) =>
			{
				IPresenter myPublicIntf = this;
				myPublicIntf.NavigateToBookmark(bmk, null, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
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

		bool IPresenter.NavigateToBookmark(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified, BookmarkNavigationOptions options)
		{
			var status = viewerPresenter.SelectMessageAt(bmk, messageMatcherWhenNoHashIsSpecified);
			if (status == Presenters.LogViewer.BookmarkSelectionStatus.Success)
				return true;
			HandleNavigateToBookmarkFailure(status, bmk, options);
			return false;
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
			DoBookmarkAction(false);
		}

		void IViewEvents.OnDeleteAllButtonClicked()
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Clear Bookmarks.");

				if (model.Bookmarks.Count == 0)
				{
					tracer.Info("Nothing to clear");
					return;
				}

				if (!view.ShowDeleteConfirmationPopup(model.Bookmarks.Count))
				{
					tracer.Info("User didn't confirm the cleaning");
					return;
				}

				model.Bookmarks.Clear();

				UpdateOverallView();
			}
		}

		void IViewEvents.OnPrevBmkButtonClicked()
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Prev Bookmark.");
				NextBookmark(false);
			}
		}

		void IViewEvents.OnNextBmkButtonClicked()
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Next Bookmark.");
				NextBookmark(true);
			}
		}

		#region Implementation

		void UpdateOverallView()
		{
			viewUpdates.RequestUpdate();
		}

		void NextBookmark(bool forward)
		{
			var firstBmk = viewerPresenter.NextBookmark(forward);
			if (firstBmk == null)
			{
				statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Bookmarks",
					forward ? "Next bookmark not found" : "Prev bookmark not found", true);
			}
			else
			{
				var firstBmkStatus = viewerPresenter.SelectMessageAt(firstBmk);
				if (firstBmkStatus != Presenters.LogViewer.BookmarkSelectionStatus.Success)
				{
					bool reportFailure = true;
					var bookmarks = model.Bookmarks.Items;
					if (!forward)
						bookmarks = bookmarks.Reverse();
					foreach (var followingBmk in bookmarks.SkipWhile(b => b != firstBmk).Skip(1))
					{
						if (viewerPresenter.SelectMessageAt(followingBmk) == Presenters.LogViewer.BookmarkSelectionStatus.Success)
						{
							reportFailure = false;
							break;
						}
					}
					if (reportFailure)
					{
						HandleNavigateToBookmarkFailure(firstBmkStatus, firstBmk,
							BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
					}
				}
			}
		}

		void HandleNavigateToBookmarkFailure(Presenters.LogViewer.BookmarkSelectionStatus status, IBookmark bmk, BookmarkNavigationOptions options)
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

			if ((status & Presenters.LogViewer.BookmarkSelectionStatus.BookmarkedMessageIsHiddenBecauseOfInvisibleThread) != 0 && bmk.Thread != null)
				statusReportFactory.CreateNewStatusReport().ShowStatusPopup(popupCaption,
					Enumerable.Repeat(new StatusReports.MessagePart(messageDescription + " belongs to a hidden thread."), 1)
					.Union(noLinks ?
						Enumerable.Empty<StatusReports.MessagePart>() :
						new StatusReports.MessagePart[] {
							new StatusReports.MessageLink("Locate", () => navHandler.ShowThread(bmk.Thread)),
							new StatusReports.MessagePart("the thread.")
						}
					), true);
			else if ((status & Presenters.LogViewer.BookmarkSelectionStatus.BookmarkedMessageIsFilteredOut) != 0)
				statusReportFactory.CreateNewStatusReport().ShowStatusPopup(popupCaption,
					Enumerable.Repeat(new StatusReports.MessagePart(messageDescription + " is hidden by display filters."), 1)
					.Union(noLinks ?
						Enumerable.Empty<StatusReports.MessagePart>() :
						new StatusReports.MessagePart[] {
							new StatusReports.MessageLink("Change", () => navHandler.ShowFiltersView()),
							new StatusReports.MessagePart("filters.")
						}
					), true);
			else if ((status & Presenters.LogViewer.BookmarkSelectionStatus.BookmarkedMessageNotFound) != 0)
				statusReportFactory.CreateNewStatusReport().ShowStatusPopup(popupCaption, messageDescription + " can not be shown", true);
		}

		private void DoBookmarkAction(bool? targetState)
		{
			IMessage l = (searchResultPresenter != null && searchResultPresenter.IsViewFocused) ? searchResultPresenter.FocusedMessage : viewerPresenter.FocusedMessage;
			if (l == null)
				return;
			var bmks = model.Bookmarks;
			if (targetState != null)
			{
				var pos = bmks.FindBookmark(bmks.Factory.CreateBookmark(l));
				if (targetState.Value == (pos.Item1 != pos.Item2))
					return;
			}
			bmks.ToggleBookmark(l);
			UpdateOverallView();
		}

		readonly IModel model;
		readonly IView view;
		readonly LJTraceSource tracer;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly BookmarksList.IPresenter listPresenter;
		readonly SearchResult.IPresenter searchResultPresenter;
		readonly StatusReports.IPresenter statusReportFactory;
		readonly IPresentersFacade navHandler;
		readonly IViewUpdates viewUpdates;

		#endregion
	};
};