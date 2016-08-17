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
		void SetPresenter(IViewEvents presenter);
		void UpdateItems(IEnumerable<ViewItem> items, ViewUpdateFlags flags);
		void RefreshFocusedMessageMark();
		IBookmark SelectedBookmark { get; }
		IEnumerable<IBookmark> SelectedBookmarks { get; }
		void Invalidate();
	};

	public struct ViewItem
	{
		public IBookmark Bookmark;
		public string Delta, AltDelta;
		public bool IsSelected;
		public bool IsEnabled;
	};

	public interface IViewEvents
	{
		void OnEnterKeyPressed();
		void OnViewDoubleClicked();
		void OnBookmarkLeftClicked(IBookmark bmk);
		void OnMenuItemClicked(ContextMenuItem item);
		ContextMenuItem OnContextMenu();
		void OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition);
		void OnCopyShortcutPressed();
		void OnDeleteButtonPressed();
		void OnSelectAllShortcutPressed();
		void OnSelectionChanged();
	};

	public interface IPresentationDataAccess
	{
		Appearance.ColoringMode Coloring { get; }
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