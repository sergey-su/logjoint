using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public delegate void BookmarkEvent(IPresenter sender, IBookmark bmk);

	public interface IPresenter
	{
		event BookmarkEvent Click;
		void SetMasterFocusedMessage(IMessage value);
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		void UpdateItems(IEnumerable<ViewItem> items);
		void RefreshFocusedMessageMark();
		IBookmark SelectedBookmark { get; }
		IEnumerable<IBookmark> SelectedBookmarks { get; }
		void SetClipboard(string text);
	};

	public struct ViewItem
	{
		public IBookmark Bookmark;
		public TimeSpan? Delta;
		public bool IsSelected;
		public bool IsEnabled;
	};

	public interface IViewEvents
	{
		void OnEnterKeyPressed();
		void OnViewDoubleClicked();
		void OnBookmarkLeftClicked(IBookmark bmk);
		void OnDeleteMenuItemClicked();
		void OnContextMenu(ref bool cancel);
		void OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition);
		void OnCopyShortcutPressed();
		void OnDeleteButtonPressed();
		void OnSelectAllShortcutPressed();
	};
};