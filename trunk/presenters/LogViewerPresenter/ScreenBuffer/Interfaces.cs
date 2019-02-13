using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	/// <summary>
	/// Maintains a buffer of log messages big enough to fill the view
	/// of given size.
	/// Interface consists of cancellable operations that modify the buffer asynchronously.
	/// Buffer stays consistent (usually unmodified) when an operation is cancelled.
	/// Only one operation at a time is possible. Before starting a new operation 
	/// previously started operations must complete or at least be cancelled.
	/// Threading: must be called from single thread associated with synchronization context 
	/// that posts completions to the same thread. UI thread meets these requirements.
	/// </summary>
	public interface IScreenBuffer
	{
		/// <summary>
		/// Updates the set of sources that the buffer gets messages from.
		/// If current set of sources is not empty and the source of current topmost line
		/// is still present in new sources set, the topmost line is preserved during the update.
		/// If the source of current topmost line is removed, the messages with nearest timestamp are loaded from remaining sources.
		/// Cancellation token <paramref name="cancellation"/> is used to interrupt reloading following sources change. Sources list is changed
		/// no matter if cancellation is triggered on not. 
		/// </summary>
		Task SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation); // todo: test deletion of topline + remaining sources
		/// <summary>
		/// Gets the list of sources previously set by <see cref="SetSources(IEnumerable{IMessagesSource}, DefaultBufferPosition, CancellationToken)"/>.
		/// </summary>
		IEnumerable<SourceScreenBuffer> Sources { get; }

		/// <summary>
		/// Updates the size of view the buffer needs to fill with log lines. Size can be zero in which case the buffer will contain one line.
		/// The topmost message is preserved.
		/// Cancellation token <paramref name="cancellation"/> is used to interrupt the loading required to change size.
		/// </summary>
		Task SetViewSize(double sz, CancellationToken cancellation); // todo: zero size supported?
		/// <summary>
		/// Currently set size of the view the ScreenBuffer has to fill with log lines.
		/// Nr of lines. Possibly fractional.
		/// </summary>
		double ViewSize { get; }

		/// <summary>
		/// Sets whether screen buffer should be filled with lines or the raw log.
		/// <seealso cref="IMessage.RawText"/>
		/// </summary>
		/// <param name="isRawMode"></param>
		void SetRawLogMode(bool isRawMode);

		/// <summary>
		/// List of log lines the buffer is filled with.
		/// </summary>
		IList<ScreenBufferEntry> Messages { get; }
		/// <summary>
		/// [0..1). The hidden part of first line. That part is above the top of the view frame.
		/// </summary>
		double TopLineScrollValue { get; set; } // todo: remove setter


		/// <summary>
		/// Loads into the screen buffer the lines surrounding given bookmark.
		/// </summary>
		Task<bool> MoveToBookmark(
			IBookmark bookmark,
			BookmarkLookupMode mode,
			CancellationToken cancellation
		);

		Task<double> ShiftBy(
			double nrOfDisplayLines,
			CancellationToken cancellation
		);
		Task Reload(
			CancellationToken cancellation
		);

		Task MoveToStreamsBegin(
			CancellationToken cancellation
		);
		Task MoveToStreamsEnd(
			CancellationToken cancellation
		);


		double BufferPosition { get; }

		Task MoveToPosition(
			double bufferPosition,
			CancellationToken cancellation
		);


		int FullyVisibleLinesCount { get; }
	};

	public interface IScreenBufferFactory
	{
		IScreenBuffer CreateScreenBuffer(double viewSize, LJTraceSource trace = null);
	};

	[Flags]
	public enum BookmarkLookupMode
	{
		MatchModeMask = 0xff,
		ExactMatch = 1,
		FindNearestBookmark = 2,
		FindNearestTime = 4, // todo: have separate kind of bookmark?

		MoveBookmarkToMiddleOfScreen = 1024
	};

	/// <summary>
	/// Represents one line of log.
	/// It can be one log message or a part of multi-line log message.
	/// </summary>
	public struct ScreenBufferEntry
	{
		/// <summary>
		/// Entry's index in ScreenBuffer's Messages collection
		/// </summary>
		public int Index;
		/// <summary>
		/// Reference to the message object. 
		/// Multiple entries can share reference to same message but differ by TextLineIndex.
		/// </summary>
		public IMessage Message;
		/// <summary>
		/// Index of a line in Message's text.
		/// </summary>
		public int TextLineIndex;
		/// <summary>
		/// Source of the message.
		/// </summary>
		public IMessagesSource Source;
	};

	public struct SourceScreenBuffer
	{
		public IMessagesSource Source;
		public long Begin;
		public long End;
	};
};