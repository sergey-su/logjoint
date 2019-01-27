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
	/// Maintains a buffer of log messages big enought to fill the view
	/// of given size.
	/// Interface consists of cancellable operations that modify the buffer asynchronously.
	/// Buffer stays consistent (usually unmodified) when an operation is cancelled.
	/// Only one operation at a time is possible. Before starting a new operation 
	/// previously started operations must complete or at least be cancelled.
	/// Threading: must be called from single thread assotiated with synchronization context 
	/// that posts completions to the same thread. UI thread meets these requirements.
	/// </summary>
	public interface IScreenBuffer
	{
		Task SetSources(IEnumerable<IMessagesSource> sources, CancellationToken cancellation);
		void SetViewSize(double sz);
		void SetRawLogMode(bool isRawMode);

		double ViewSize { get; }
		int FullyVisibleLinesCount { get; }
		IList<ScreenBufferEntry> Messages { get; }
		IEnumerable<SourceScreenBuffer> Sources { get; }
		double TopLineScrollValue { get; set; }
		double BufferPosition { get; }
		bool ContainsSource(IMessagesSource source);

		Task MoveToStreamsBegin(
			CancellationToken cancellation
		);
		Task MoveToStreamsEnd(
			CancellationToken cancellation
		);
		Task<bool> MoveToBookmark(
			IBookmark bookmark,
			BookmarkLookupMode mode,
			CancellationToken cancellation
		);
		Task<int> ShiftBy(
			double nrOfDisplayLines,
			CancellationToken cancellation
		);
		Task Reload(
			CancellationToken cancellation
		);
		Task MoveToPosition(
			double bufferPosition,
			CancellationToken cancellation
		);
	};

	public interface IScreenBufferFactory
	{
		IScreenBuffer CreateScreenBuffer(InitialBufferPosition initialBufferPosition, LJTraceSource trace);
	};

	[Flags]
	public enum BookmarkLookupMode
	{
		MatchModeMask = 0xff,
		ExactMatch = 1,
		FindNearestBookmark = 2,
		FindNearestTime = 4,

		MoveBookmarkToMiddleOfScreen = 1024
	};

	/// <summary>
	/// Represents one line of log.
	/// It can be one log message or a part of multiline log message.
	/// </summary>
	public struct ScreenBufferEntry
	{
		/// <summary>
		/// Entry's index in ScreenBuffer's Messages collection
		/// </summary>
		public int Index;
		/// <summary>
		/// Reference to the message object. 
		/// Multiple entries can share refernce to same message but differ by TextLineIndex.
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

	public enum InitialBufferPosition
	{
		StreamsBegin,
		StreamsEnd,
		Nowhere
	};
};