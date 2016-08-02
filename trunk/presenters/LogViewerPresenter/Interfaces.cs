using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IPresenter
	{
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
		bool NavigationIsInProgress { get; }

		Task<bool> SelectMessageAt(IBookmark bmk);
		Task SelectMessageAt(DateTime date, ILogSource preferredSource);
		Task GoHome();
		Task GoToEnd();
		Task GoToNextMessageInThread();
		Task GoToPrevMessageInThread();
		Task GoToNextHighlightedMessage();
		Task GoToPrevHighlightedMessage();
		Task GoToNextMessage();
		Task GoToPrevMessage();
		Task<SearchResult> Search(SearchOptions opts);

		IBookmark NextBookmark(bool forward);
		void ToggleBookmark(IMessage line);

		IMessage FocusedMessage { get; }
		IMessage SlaveModeFocusedMessage { get; set; }
		DateTime? FocusedMessageTime { get; } // todo: remove
		SelectionInfo Selection { get; } // todo: remove. have IsSingleLineSelection
		void ClearSelection();
		string GetSelectedText();
		void CopySelectionToClipboard();
		Task SelectSlaveModeFocusedMessage();
		void SelectFirstMessage();
		void SelectLastMessage();

		void InvalidateView(); // todo: remove

		event EventHandler SelectionChanged;
		event EventHandler FocusedMessageChanged;
		event EventHandler DefaultFocusedMessageAction;
		event EventHandler ManualRefresh;
		event EventHandler RawViewModeChanged;
		event EventHandler ColoringModeChanged;
		event EventHandler NavigationIsInProgressChanged;
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

	[Flags]
	public enum Key
	{
		KeyCodeMask = 0xff,
		None = 0,
		Refresh,
		Up, Down, Left, Right,
		PageUp, PageDown,
		ContextMenu,
		Enter,
		Copy,
		BeginOfLine, EndOfLine,
		BeginOfDocument, EndOfDocument,
		BookmarkShortcut,

		ModifySelectionModifier = 512,
		AlternativeModeModifier = 1024,
		JumpOverWordsModifier = 2048
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

	public interface IViewFonts
	{
		string[] AvailablePreferredFamilies { get; }
		KeyValuePair<LogFontSize, int>[] FontSizes { get; }
	};

	public interface IView : IViewFonts
	{
		void SetViewEvents(IViewEvents viewEvents);
		void SetPresentationDataAccess(IPresentationDataAccess presentationDataAccess);
		float DisplayLinesPerPage { get; }
		void SetVScroll(double? value);

		// todo: review if methods below are still valid
		void UpdateFontDependentData(string fontName, LogFontSize fontSize);
		void SaveViewScrollState(SelectionInfo selection);
		void RestoreViewScrollState(SelectionInfo selection);
		void HScrollToSelectedText(SelectionInfo selection);
		object GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection);
		void PopupContextMenu(object contextMenuPopupData);
		void Invalidate();
		void InvalidateMessage(DisplayLine line);
		void DisplayNothingLoadedMessage(string messageToDisplayOrNull);
		void RestartCursorBlinking();
		void UpdateMillisecondsModeDependentData();
		void AnimateSlaveMessagePosition();
	};

	public interface IViewEvents
	{
		void OnDisplayLinesPerPageChanged();
		void OnIncrementalVScroll(float nrOfDisplayLines);
		void OnVScroll(double value, bool isRealtimeScroll);

		void OnHScroll();
		void OnMouseWheelWithCtrl(int delta);
		void OnCursorTimerTick();
		void OnKeyPressed(Key k);
		void OnMenuOpening(out ContextMenuItem visibleItems, out ContextMenuItem checkedItems, out string defaultItemText);
		void OnMenuItemClicked(ContextMenuItem menuItem, bool? itemChecked = null);
		void OnMessageMouseEvent(
			CursorPosition pos, // todo: remove CursorPosition from intf
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData);
	};

	public interface IPresentationDataAccess
	{
		int DisplayLinesCount { get; }
		IEnumerable<DisplayLine> GetDisplayLines(int beginIdx, int endIdx);
		double GetFirstDisplayMessageScrolledLines();
		bool ShowTime { get; }
		bool ShowMilliseconds { get; }
		bool ShowRawMessages { get; }
		SelectionInfo Selection { get; }
		ColoringMode Coloring { get; }
		FocusedMessageDisplayModes FocusedMessageDisplayMode { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler1 { get; }
		Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler2 { get; }
		Tuple<int, int> FindSlaveModeFocusedMessagePosition(int beginIdx, int endIdx);

		// todo: make view unaware of bookmarks handler
		IBookmarksHandler CreateBookmarksHandler();
	};

	public interface IMessagesSource
	{
		Task<DateBoundPositionResponseData> GetDateBoundPosition(
			DateTime d,
			ListUtils.ValueBound bound,
			LogProviderCommandPriority priority,
			CancellationToken cancellation
		);
		Task EnumMessages(
			long fromPosition,
			Func<IMessage, bool> callback,
			EnumMessagesFlag flags,
			LogProviderCommandPriority priority,
			CancellationToken cancellation
		);
		FileRange.Range PositionsRange { get; }
		DateRange DatesRange { get; }

		FileRange.Range ScrollPositionsRange { get; }
		long MapPositionToScrollPosition(long pos);
		long MapScrollPositionToPosition(long pos);
	};

	public interface IModel
	{
		IEnumerable<IMessagesSource> Sources { get; }
		IModelThreads Threads { get; }
		IFiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		string MessageToDisplayWhenMessagesCollectionIsEmpty { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }

		event EventHandler OnSourcesChanged;
		event EventHandler OnSourceMessagesChanged;
		event EventHandler OnLogSourceColorChanged;
	};

	public interface ISearchResultModel : IModel
	{
		SearchAllOccurencesParams SearchParams { get; } // todo: how to hande that with multiple search results?
	};
};