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
		bool CursorState { get; }
	};

	internal delegate int MessageTextLinesMapper (int lineIdx);

	/// <summary>
	/// Holds information about the text that is displayed for a given log message.
	/// It can be computed and be different from any of intrinsic message's texts.
	/// Example of computed text: search results view.
	/// </summary>
	internal struct MessageDisplayTextInfo
	{
		public MultilineText DisplayText;
		/// <summary>
		/// Maps <see cref="DisplayText"/>'s line indexes to that of original text.
		/// </summary>
		public MessageTextLinesMapper LinesMapper;
		/// <summary>
		/// Maps text lines of original message text to line of <see cref="DisplayText"/>.
		/// </summary>
		public MessageTextLinesMapper ReverseLinesMapper;
	};

	internal interface IPresentationProperties
	{
		bool ShowTime { get; }
		bool ShowMilliseconds { get; }
		ColoringMode Coloring { get; }
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
		ScrollToViewEventIfSelectionDidNotChange = 128,
		CrosslineNavigation = 256,
	};
};