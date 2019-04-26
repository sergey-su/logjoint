using System;
using System.Threading.Tasks;
using static LogJoint.Settings.Appearance;

namespace LogJoint.UI.Presenters.LogViewer
{
	internal interface ISelectionManager: IDisposable
	{
		SelectionInfo Selection { get; }
		/// <summary>
		/// Index of view line that cursor belongs. null is cursor position is outside of the screen or
		/// no cursor position is set.
		/// </summary>
		int? CursorViewLine { get; }
		/// <summary>
		/// Range of view lines indexes corresponding to being and end of the selection.
		/// Each index is either a valid screen buffer index,
		/// or -1, if selection boundary is located before screen buffer,
		/// of screen buffer size, if selection boundary is located after screen buffer.
		/// </summary>
		(int, int) ViewLinesRange { get; }

		void SetSelection(int displayIndex, SelectionFlag flag = SelectionFlag.None, int? textCharIndex = null);
		void SetSelection(int displayIndex, int textCharIndex1, int textCharIndex2);
		bool SelectWordBoundaries(ViewLine viewLine, int charIndex);
		Task CopySelectionToClipboard();
		Task<string> GetSelectedText();
		IBookmark GetFocusedMessageBookmark();
		bool CursorState { get; }

		event EventHandler SelectionChanged;
		event EventHandler FocusedMessageChanged;
		event EventHandler FocusedMessageBookmarkChanged;
	};

	internal delegate int MessageTextLinesMapper (int lineIdx);

	internal interface IPresentationProperties
	{
		bool ShowTime { get; }
		bool ShowMilliseconds { get; }
		ColoringMode Coloring { get; }
		bool RawMessageViewMode { get; }
		MessageTextLinesMapper GetDisplayTextLinesMapper(IMessage msg);
	};

	[Flags]
	internal enum SelectionFlag
	{
		None = 0,
		PreserveSelectionEnd = 1,
		SelectBeginningOfLine = 2,
		SelectEndOfLine = 4,
		SelectBeginningOfNextWord = 8,
		SelectBeginningOfPrevWord = 16,
		SuppressOnFocusedMessageChanged = 32,
		NoHScrollToSelection = 64,
		ScrollToViewEventIfSelectionDidNotChange = 128
	};
};