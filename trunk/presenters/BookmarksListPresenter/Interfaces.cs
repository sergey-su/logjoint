using System;
using System.Collections.Generic;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public delegate void BookmarkEvent(IPresenter sender, IBookmark bmk);

	public interface IPresenter
	{
		event BookmarkEvent Click;
		void SetMasterFocusedMessage(IBookmark focusedMessageBookmark);
		void DeleteSelectedBookmarks();
	};

	public interface IView
	{
		void SetPresenter(IViewModel presenter);
		void UpdateItems(IEnumerable<ViewItem> items, ViewUpdateFlags flags);
		void RefreshFocusedMessageMark();
		ViewItem? SelectedBookmark { get; }
		IEnumerable<ViewItem> SelectedBookmarks { get; }
		void Invalidate();
	};

	public struct ViewItem
	{
		public string Delta, AltDelta;
		public bool IsSelected;
		public bool IsEnabled;
		public string Text;
		public ModelColor? ContextColor;

		internal IBookmark Bookmark;
	};

	public interface IViewModel
	{
		string FontName { get; }
		ColorThemeMode Theme { get; }

		void OnEnterKeyPressed();
		void OnViewDoubleClicked();
		void OnBookmarkLeftClicked(ViewItem item);
		void OnMenuItemClicked(ContextMenuItem item);
		ContextMenuItem OnContextMenu();
		void OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition);
		void OnCopyShortcutPressed();
		void OnDeleteButtonPressed();
		void OnSelectAllShortcutPressed();
		void OnSelectionChanged();
	};

	[Flags]
	public enum ContextMenuItem
	{
		None = 0,
		Delete = 1,
		Copy = 2,
		CopyWithDeltas = 4
	};

	[Flags]
	public enum ViewUpdateFlags
	{
		None = 0,
		SelectionDidNotChange = 1,
		ItemsCountDidNotChange = 2
	};
};