using System;
using System.Collections.Generic;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using System.Threading.Tasks;
using System.Collections.Immutable;
using LogJoint.Drawing;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IPresenterInternal: IPresenter, IDisposable
	{
		LogFontSize FontSize { get; set; }
		string FontName { get; set; }
		PreferredDblClickAction DblClickAction { get; set; }
		FocusedMessageDisplayModes FocusedMessageDisplayMode { get; set; }
		string DefaultFocusedMessageActionCaption { get; set; }
		bool ShowTime { get; set; }
		bool ShowMilliseconds { get; set; }
		bool ShowRawMessages { get; set; }
		bool RawViewAllowed { get; }
		bool ViewTailMode { get; set; }
		UserInteraction DisabledUserInteractions { get; set; }
		ColoringMode Coloring { get; set; }
		bool NavigationIsInProgress { get; }

		Task<bool> SelectMessageAt(IBookmark bmk);
		Task SelectMessageAt(DateTime date, ILogSource[] preferredSources);
		Task GoToNextMessageInThread();
		Task GoToPrevMessageInThread();
		Task GoToNextHighlightedMessage();
		Task GoToPrevHighlightedMessage();
		Task GoToNextMessage();
		Task GoToPrevMessage();
		Task<IMessage> Search(SearchOptions opts);
		void SelectFirstMessage();
		void SelectLastMessage();
		void MakeFirstLineFullyVisible();

		IBookmark FocusedMessageBookmark { get; }
		Task<Dictionary<IMessagesSource, long>> GetCurrentPositions(CancellationToken cancellation);
		IBookmark SlaveModeFocusedMessage { get; set; }
		Task SelectSlaveModeFocusedMessage();

		Task<string> GetSelectedText(); // func is async when selected text is not on the screen atm
		Task CopySelectionToClipboard();
		bool IsSinglelineNonEmptySelection { get; }

		bool HasInputFocus { get; }
		void ReceiveInputFocus();

		event EventHandler FocusedMessageChanged;
		event EventHandler FocusedMessageBookmarkChanged;
		event EventHandler DefaultFocusedMessageAction;
		event EventHandler ManualRefresh;
		event EventHandler<ContextMenuEventArgs> ContextMenuOpening;
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
		NextHighlightedMessage,
		PrevHighlightedMessage,

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

	[DebuggerDisplay("{TextLineValue}")]
	public struct ViewLine
	{
		public int LineIndex;
		public string Time;
		public string TextLineValue;
		public bool IsBookmarked;
		public SeverityIcon Severity;
		public Color? ContextColor;
		public (int, int)? SelectedBackground;
		public int? CursorCharIndex;
		public bool CursorVisible;
		public bool HasMessageSeparator;
		public IEnumerable<(int, int, Color)> SearchResultHighlightingRanges => searchResultHighlightingHandler?.GetHighlightingRanges(this);
		public IEnumerable<(int, int, Color)> SelectionHighlightingRanges => selectionHighlightingHandler?.GetHighlightingRanges(this);
		public IEnumerable<(int, int, Color)> HighlightingFiltersHighlightingRanges => highlightingFiltersHandler?.GetHighlightingRanges(this);

		internal IMessage Message;
		internal int TextLineIndex;
		internal MultilineText Text;
		internal IHighlightingHandler searchResultHighlightingHandler;
		internal IHighlightingHandler selectionHighlightingHandler;
		internal IHighlightingHandler highlightingFiltersHandler;

		public static (int relativeOrder, bool changed) Compare(ViewLine vl1, ViewLine vl2)
		{
			int cmp = MessagesComparer.Compare(vl1.Message, vl2.Message);
			if (cmp != 0)
				return (cmp, false);
			cmp = vl1.TextLineIndex - vl2.TextLineIndex;
			if (cmp != 0)
				return (cmp, false);
			var unchanged =
				vl1.IsBookmarked == vl2.IsBookmarked
			 && vl1.SelectedBackground == vl2.SelectedBackground
			 && vl1.ContextColor == vl2.ContextColor
			 && vl1.CursorCharIndex == vl2.CursorCharIndex
			 && vl1.TextLineValue == vl2.TextLineValue
			 && vl1.Time == vl2.Time;
			// todo: filters
			return (0, unchanged);
		}
	};

	public enum SeverityIcon
	{
		None,
		Error,
		Warning
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
		void SetViewModel(IViewModel viewEvents);
		float DisplayLinesPerPage { get; }
		void HScrollToSelectedText(int charIndex);
		bool HasInputFocus { get; }
		void ReceiveInputFocus();
		object GetContextMenuPopupData(int? viewLineIndex);
		void PopupContextMenu(object contextMenuPopupData);
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }

		void OnIncrementalVScroll(float nrOfDisplayLines);
		void OnVScroll(double value, bool isRealtimeScroll);
		void OnHScroll();
		void OnMouseWheelWithCtrl(int delta);
		void OnKeyPressed(Key k);
		MenuData OnMenuOpening();
		void OnMenuItemClicked(ContextMenuItem menuItem, bool? itemChecked = null);
		void OnMessageMouseEvent(
			ViewLine line,
			int charIndex,
			MessageMouseEventFlag flags,
			object preparedContextMenuPopupData);
		void OnDrawingError(Exception e);

		/// <summary>
		/// Collection of lines that should be displayed on the view.
		/// Whenever the collection changes, view needs to be re-rendered.
		/// </summary>
		ImmutableArray<ViewLine> ViewLines { get; }
		string ViewLinesAggregaredText { get; }
		double FirstDisplayMessageScrolledLines { get; }
		/// <summary>
		/// Max length of string representing view line time. <see cref="ViewLine.Time"/>.
		/// Zero if time should not be rendered.
		/// </summary>
		int TimeMaxLength { get; }
		/// <summary>
		/// Returns null if focused message mark is not visible.
		/// Returns array with one number if focused message mark is large (master view). 0-th item is view line index.
		/// Returns array with three numbers if focused message mark is small (slave view). 0-th and 1-st items are view line indexes. 2-nd item is animation stage.
		/// </summary>
		int[] FocusedMessageMark { get; }
		FontData Font { get; }
		LJTraceSource Trace { get; }
		double? VerticalScrollerPosition { get; }
		string EmptyViewMessage { get; }
		ColorThemeMode ColorTheme { get; }
	};

	public class MenuData
	{
		public ContextMenuItem VisibleItems;
		public ContextMenuItem CheckedItems;
		public string DefaultItemText;
		public class ExtendedItem
		{
			public string Text;
			public Action Click;
		};
		public List<ExtendedItem> ExtendededItems;
	};

	public class FontData
	{
		public string Name { get; private set; }
		public LogFontSize Size { get; private set; }
		public FontData(string name = null, LogFontSize size = LogFontSize.Normal)
		{
			Name = name;
			Size = size;
		}
	};

	public interface IMessagesSource
	{
		Task<DateBoundPositionResponseData> GetDateBoundPosition(
			DateTime d,
			ValueBound bound,
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

		/// <summary>
		/// Returns log source if all messages from this messages source belong to it.
		/// Otherwise returns null.
		/// </summary>
		ILogSource LogSourceHint { get; }

		/// <summary>
		/// Returns true if the source serves messages without gaps in positions,
		/// i.e. EndPosition of previous message equals Position of next one.
		/// Example of a source that returns false is the source that represents
		/// search results which can be sparse.
		/// </summary>
		bool HasConsecutiveMessages { get; }
	};

	public interface IModel
	{
		IEnumerable<IMessagesSource> Sources { get; }

		event EventHandler OnSourcesChanged;
		event EventHandler<SourceMessagesChangeArgs> OnSourceMessagesChanged;
		event EventHandler OnLogSourceColorChanged;
	};

	public class SourceMessagesChangeArgs : EventArgs
	{
		public bool IsIncrementalChange { get; private set; }

		public SourceMessagesChangeArgs(bool isIncrementalChange)
		{
			this.IsIncrementalChange = isIncrementalChange;
		}
	};

	public interface ISearchResultModel : IModel
	{
		IFiltersList SearchFiltersList { get; }
		void RaiseSourcesChanged();
	};

	public interface IPresenterFactory
	{
		IPresenterInternal CreateLoadedMessagesPresenter(IView view);
		(IPresenterInternal Presenter, ISearchResultModel Model) CreateSearchResultsPresenter(
			IView view, IPresenterInternal loadedMessagesPresenter);
		IPresenterInternal CreateIsolatedPresenter(IModel model, IView view, IColorTheme theme = null);
	};

	public class ContextMenuEventArgs : EventArgs
	{
		internal List<MenuData.ExtendedItem> items;

		public void Add(MenuData.ExtendedItem item)
		{
			if (items == null)
				items = new List<MenuData.ExtendedItem>();
			items.Add(item);
		}
	};

	public interface IViewModeStrategy: IDisposable
	{
		bool IsRawMessagesMode { get; set; }
		bool IsRawMessagesModeAllowed { get; }
	};

	public interface IColoringModeStrategy : IDisposable
	{
		ColoringMode Coloring { get; set; }
	};
};