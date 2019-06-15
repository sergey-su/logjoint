using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
			IPresentersFacade navHandler,
			IColorTheme theme,
			IChangeNotification changeNotification
		)
		{
			this.hlFilters = hlFilters;
			this.bookmarks = bookmarks;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.navHandler = navHandler;
			this.changeNotification = changeNotification;

			this.getFocusedMessage = Selectors.Create(() => viewerPresenter.FocusedMessage,
				message => message?.GetLogSource() == null ? null : message);
			var getBookmarkData = bookmarks == null ? () => (null, null) :
				Selectors.Create(getFocusedMessage, () => bookmarks.Items, (focused, bmks) =>
				{
					if (focused == null)
						return (noSelection, null);
					var isBookmarked = IsMessageBookmarked(focused);
					return (isBookmarked ? "yes" : "no", isBookmarked ? "clear bookmark" : "set bookmark");
				});
			bool getHlFilteringEnabled() => hlFilters.FilteringEnabled && hlFilters.Items.Count > 0;
			List<ContentViewMode> getContentViewModes(IMessage msg)
			{
				var contentViewModes = new List<ContentViewMode>();
				if (msg != null)
				{
					contentViewModes.Add(ContentViewMode.Summary);
					if (msg.RawText.IsInitialized)
						contentViewModes.Add(ContentViewMode.RawText);
				}
				return contentViewModes;
			};
			this.getDialogData = Selectors.Create(
				getFocusedMessage,
				getBookmarkData,
				getHlFilteringEnabled,
				() => contentViewMode,
				(message, bmk, hlEnabled, setContentViewMode) =>
			{
				var (bookmarkedStatus, bookmarkAction) = bmk;
				ILogSource ls = message?.GetLogSource();
				var contentViewModes = getContentViewModes(message);
				int? effectiveContentViewMode =
					contentViewModes.Count == 0 ? new int?() :
					RangeUtils.PutInRange(0, contentViewModes.Count, setContentViewMode);
				return new DialogData()
				{
					TimeValue = message != null ? message.Time.ToUserFrendlyString() : noSelection,

					ThreadLinkValue = message != null ? message.Thread.DisplayName : noSelection,
					ThreadLinkBkColor = theme.ThreadColors.GetByIndex(message?.Thread?.ThreadColorIndex),
					ThreadLinkEnabled = message != null && navHandler?.CanShowThreads == true,

					SourceLinkValue = ls != null ? ls.DisplayName : noSelection,
					SourceLinkBkColor = theme.ThreadColors.GetByIndex(ls?.ColorIndex),
					SourceLinkEnabled = ls != null && navHandler != null,

					BookmarkedStatusText = bookmarkedStatus ?? "N/A",
					BookmarkActionLinkText = bookmarkAction,
					BookmarkActionLinkEnabled = !string.IsNullOrEmpty(bookmarkAction),

					SeverityValue = (message?.Severity)?.ToString() ?? noSelection,

					ContentViewModes = ImmutableArray.CreateRange(contentViewModes.Select(m => m.Name)),
					ContentViewModeIndex = effectiveContentViewMode,
					TextValue = effectiveContentViewMode == null ? "" :
						contentViewModes[effectiveContentViewMode.Value].TextGetter(message).Text.Value,

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
			if (msg?.GetLogSource() != null && navHandler != null)
				navHandler.ShowLogSource(msg.GetLogSource());
		}

		void IDialogViewModel.OnContentViewModeChange(int value)
		{
			if (value != contentViewMode)
			{
				contentViewMode = value;
				changeNotification.Post();
			}
		}

		IDialog GetPropertiesForm()
		{
			if (propertiesForm != null)
				if (propertiesForm.IsDisposed)
					propertiesForm = null;
			return propertiesForm;
		}

		class ContentViewMode
		{
			public string Name;
			public MessageTextGetter TextGetter;

			public static readonly ContentViewMode Summary = new ContentViewMode()
			{
				Name = "Summary",
				TextGetter = MessageTextGetters.SummaryTextGetter,
			};
			public static readonly ContentViewMode RawText = new ContentViewMode()
			{
				Name = "Raw text",
				TextGetter = MessageTextGetters.RawTextGetter,
			};
		};

		readonly IChangeNotification changeNotification;
		readonly IFiltersList hlFilters;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly IPresentersFacade navHandler;
		readonly Func<IMessage> getFocusedMessage;
		readonly Func<DialogData> getDialogData;
		int contentViewMode;
		IDialog propertiesForm;
		static readonly string noSelection = "<no selection>";
	};
};