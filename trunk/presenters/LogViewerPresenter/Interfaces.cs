using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IPresenter
	{
		IMessage FocusedMessage { get; }
		DateTime? FocusedMessageTime { get; }
		LogFontSize FontSize { get; set; }
		string FontName { get; set; }
		PreferredDblClickAction DblClickAction { get; set; }
		FocusedMessageDisplayModes FocusedMessageDisplayMode { get; set; }
		string DefaultFocusedMessageActionCaption { get; set; }
		bool ShowTime { get; set; }
		bool ShowMilliseconds { get; set; }
		bool ShowRawMessages { get; set; }
		bool RawViewAllowed { get; set; }
		UserInteraction DisabledUserInteractions { get; set; }
		ColoringMode Coloring { get; set; }
		SearchResult Search(SearchOptions opts);
		bool BookmarksAvailable { get; }
		void ToggleBookmark(IMessage line);
		void SelectMessageAt(DateTime date, NavigateFlag alignFlag, ILogSource preferredSource);
		void GoToParentFrame();
		void GoToEndOfFrame();
		void GoToNextMessageInThread();
		void GoToPrevMessageInThread();
		void GoToNextHighlightedMessage();
		void GoToPrevHighlightedMessage();
		SelectionInfo Selection { get; }
		void UpdateView();
		void InvalidateView();
		IBookmark NextBookmark(bool forward);
		BookmarkSelectionStatus SelectMessageAt(IBookmark bmk);
		BookmarkSelectionStatus SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified);
		void Next();
		void Prev();
		void ClearSelection();
		string GetSelectedText();
		void CopySelectionToClipboard();
		IMessage SlaveModeFocusedMessage { get; set; }
		void SelectSlaveModeFocusedMessage();
		IMessagesCollection LoadedMessages { get; }
		void SelectFirstMessage();
		void SelectLastMessage();



		event EventHandler SelectionChanged;
		event EventHandler FocusedMessageChanged;
		event EventHandler BeginShifting;
		event EventHandler EndShifting;
		event EventHandler DefaultFocusedMessageAction;
		event EventHandler ManualRefresh;
		event EventHandler RawViewModeChanged;
		event EventHandler ColoringModeChanged;
	};

	[Flags]
	public enum UserInteraction
	{
		None = 0,
		RawViewSwitching = 1,
		FontResizing = 2,
		FramesNavigationMenu = 4,
		CopyMenu = 8,
		CopyShortcut = 16,
	};

	public enum Key
	{
		None,
		F5,
		Up, Down, Left, Right,
		PageUp, PageDown,
		Apps,
		Enter,
		Copy,
		Home,
		End,
		B
	};

	[Flags]
	public enum ContextMenuItem
	{
		None = 0,
		Copy = 1,
		CollapseExpand = 2,
		RecursiveCollapseExpand = 4,
		GotoParentFrame = 8,
		GotoEndOfFrame = 16,
		ShowTime = 32,
		ShowRawMessages = 64,
		DefaultAction = 128,
		ToggleBmk = 256,
		GotoNextMessageInTheThread = 512,
		GotoPrevMessageInTheThread = 1024,
		CollapseAllFrames = 2048,
		ExpandAllFrames = 4096
	};

	public enum FocusedMessageDisplayModes
	{
		Master,
		Slave
	};

	public struct DisplayLine
	{
		public int DisplayLineIndex;
		public IMessage Message;
		public int TextLineIndex;
	};

	[Flags]
	public enum MessageMouseEventFlag
	{
		None = 0,

		SingleClick = 1,
		DblClick = 2,
		CapturedMouseMove = 4,

		RightMouseButton = 8,

		OulineBoxesArea = 16,

		ShiftIsHeld = 32,
		AltIsHeld = 64,
		CtrlIsHeld = 128
	};

	public enum PreferredDblClickAction
	{
		SelectWord,
		DoDefaultAction
	};

	[Flags]
	public enum BookmarkSelectionStatus
	{
		Success = 0,
		BookmarkedMessageNotFound = 1,
		BookmarkedMessageIsFilteredOut = 2,
		BookmarkedMessageIsHiddenBecauseOfInvisibleThread = 4
	};

	public interface IViewFonts
	{
		string[] AvailablePreferredFamilies { get; }
		KeyValuePair<LogFontSize, int>[] FontSizes { get; }
	};

	public interface IView : IViewFonts
	{
		void SetViewEvents(IViewEvents viewEvents);
		void SetPresentationDataAccess(IPresentationDataAccess presentationDataAccess);
		void UpdateFontDependentData(string fontName, LogFontSize fontSize);
		void SaveViewScrollState(SelectionInfo selection);
		void RestoreViewScrollState(SelectionInfo selection);
		void HScrollToSelectedText(SelectionInfo selection);
		object GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection);
		void PopupContextMenu(object contextMenuPopupData);
		void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage);
		void UpdateScrollSizeToMatchVisibleCount();
		void Invalidate();
		void InvalidateMessage(DisplayLine line);
		void SetClipboard(string text);
		void DisplayEverythingFilteredOutMessage(bool displayOrHide);
		void DisplayNothingLoadedMessage(string messageToDisplayOrNull);
		void RestartCursorBlinking();
		void UpdateMillisecondsModeDependentData();
		int DisplayLinesPerPage { get; }
		void AnimateSlaveMessagePosition();
	};

	public interface IViewEvents
	{
		void OnMouseWheelWithCtrl(int delta);
		void OnCursorTimerTick();
		void OnShowFiltersLinkClicked();
		void OnSearchNotFilteredMessageLinkClicked(bool searchUp);
		void OnKeyPressed(Key k, bool ctrl, bool alt, bool shift);
		void OnMenuOpening(out ContextMenuItem visibleItems, out ContextMenuItem checkedItems, out string defaultItemText);
		void OnMenuItemClicked(ContextMenuItem menuItem, bool? itemChecked = null);
		void OnHScrolled();
		bool OnOulineBoxClicked(IMessage msg, bool controlIsHeld);
		void OnMessageMouseEvent(
			CursorPosition pos,
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData);
	};

	public interface IPresentationDataAccess
	{
		bool ShowTime { get; }
		bool ShowMilliseconds { get; }
		bool ShowRawMessages { get; }
		SelectionInfo Selection { get; }
		ColoringMode Coloring { get; }
		FocusedMessageDisplayModes FocusedMessageDisplayMode { get; }
		IMessagesCollection DisplayMessages { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler1 { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler2 { get; }
		Tuple<int, int> FindSlaveModeFocusedMessagePosition(int beginIdx, int endIdx);
		IEnumerable<DisplayLine> GetDisplayLines(int beginIdx, int endIdx);
		IBookmarksHandler CreateBookmarksHandler();
	};

	public interface IModel
	{
		IMessagesCollection Messages { get; }
		IModelThreads Threads { get; }
		IFiltersList DisplayFilters { get; }
		IFiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		string MessageToDisplayWhenMessagesCollectionIsEmpty { get; }
		void ShiftUp();
		bool IsShiftableUp { get; }
		void ShiftDown();
		bool IsShiftableDown { get; }
		void ShiftAt(DateTime t);
		void ShiftHome();
		void ShiftToEnd();
		bool GetAndResetPendingUpdateFlag();
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }

		event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
	};

	public interface ISearchResultModel : IModel
	{
		SearchAllOccurencesParams SearchParams { get; }
	};
};