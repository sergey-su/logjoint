using System;
using System.Linq;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
	public class Presenter : IPresenter, IDialogViewModel
	{
		public Presenter(
			IBookmarks bookmarks,
			IFiltersList hlFilters,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			IPresentersFacade navHandler)
		{
			this.hlFilters = hlFilters;
			this.bookmarks = bookmarks;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.navHandler = navHandler;

			this.getFocusedMessage = Selectors.Create(() => viewerPresenter.FocusedMessage,
				message => message?.LogSource?.IsDisposed == true ? null : message);
			var getBookmarkData = bookmarks == null ? () => (null, null) :
				Selectors.Create(getFocusedMessage, () => bookmarks.Items, (focused, bmks) =>
				{
					if (focused == null)
						return (noSelection, null);
					var isBookmarked = IsMessageBookmarked(focused);
					return (isBookmarked ? "yes" : "no", isBookmarked ? "clear bookmark" : "set bookmark");
				});
			bool getHlFilteringEnabled() => hlFilters.FilteringEnabled && hlFilters.Count > 0;
			this.getDialogData = Selectors.Create(getFocusedMessage, getBookmarkData, getHlFilteringEnabled, (message, bmk, hlEnabled) =>
			{
				var (bookmarkedStatus, bookmarkAction) = bmk;
				ILogSource ls = message.GetLogSource();
				return new DialogData()
				{
					TimeValue = message != null ? message.Time.ToUserFrendlyString() : noSelection,

					ThreadLinkValue = message != null ? message.Thread.DisplayName : noSelection,
					ThreadLinkBkColor = message?.Thread?.ThreadColor,
					ThreadLinkEnabled = message != null && navHandler?.CanShowThreads == true,

					SourceLinkValue = ls != null ? ls.DisplayName : noSelection,
					SourceLinkBkColor = ls?.Color,
					SourceLinkEnabled = ls != null && navHandler != null,

					BookmarkedStatusText = bookmarkedStatus ?? "N/A",
					BookmarkActionLinkText = bookmarkAction,
					BookmarkActionLinkEnabled = !string.IsNullOrEmpty(bookmarkAction),

					SeverityValue = (message?.Severity)?.ToString() ?? noSelection,

					TextValue = (message?.Text)?.Value ?? "",

					HighlightedCheckboxEnabled = hlEnabled
				};
			});
		}

		void IPresenter.ShowDialog()
		{
			if (GetPropertiesForm() == null)
			{
				propertiesForm = view.CreateDialog(this);
			}
			propertiesForm.Show();
		}

		DialogData IDialogViewModel.Data
		{
			get { return getDialogData(); }
		}

		bool IsMessageBookmarked(IMessage msg)
		{
			return bookmarks != null && bookmarks.GetMessageBookmarks(msg).Length > 0;
		}

		void IDialogViewModel.OnBookmarkActionClicked()
		{
			var msg = getFocusedMessage();
			if (msg == null)
				return;
			var msgBmks = bookmarks.GetMessageBookmarks(msg);
			if (msgBmks.Length == 0)
				bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(msg, 0));
			else foreach (var b in msgBmks)
				bookmarks.ToggleBookmark(b);
		}

		void IDialogViewModel.OnNextClicked(bool highlightedChecked)
		{
			if (highlightedChecked)
				viewerPresenter.GoToNextHighlightedMessage().IgnoreCancellation();
			else
				viewerPresenter.GoToNextMessage().IgnoreCancellation();
		}

		void IDialogViewModel.OnPrevClicked(bool highlightedChecked)
		{
			if (highlightedChecked)
				viewerPresenter.GoToPrevHighlightedMessage().IgnoreCancellation();
			else
				viewerPresenter.GoToPrevMessage().IgnoreCancellation();
		}

		void IDialogViewModel.OnThreadLinkClicked()
		{
			var msg = getFocusedMessage();
			if (msg != null && navHandler != null)
				navHandler.ShowThread(msg.Thread);
		}

		void IDialogViewModel.OnSourceLinkClicked()
		{
			var msg = getFocusedMessage();
			if (msg?.LogSource != null && navHandler != null)
				navHandler.ShowLogSource(msg.LogSource);
		}

		IDialog GetPropertiesForm()
		{
			if (propertiesForm != null)
				if (propertiesForm.IsDisposed)
					propertiesForm = null;
			return propertiesForm;
		}

		readonly IFiltersList hlFilters;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly IPresentersFacade navHandler;
		readonly Func<IMessage> getFocusedMessage;
		readonly Func<DialogData> getDialogData;
		IDialog propertiesForm;
		static readonly string noSelection = "<no selection>";
	};
};