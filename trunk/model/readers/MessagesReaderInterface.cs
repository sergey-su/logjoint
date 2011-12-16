using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	[Flags]
	public enum UpdateBoundsStatus
	{
		NothingUpdated = 0,
		NewMessagesAvailable = 1,
		OldMessagesAreInvalid = 2
	};

	public enum MessagesParserDirection
	{
		Forward,
		Backward
	};

	[Flags]
	public enum MessagesParserFlag
	{
		None = 0,
		Default = 0,
		HintParserWillBeUsedForMassiveSequentialReading = 1,
		HintMessageTimeIsNotNeeded = 2,
		HintMessageContentIsNotNeeed = 4,
		DisableDejitter = 8,
		DisableMultithreading = 16,
	};

	public struct CreateParserParams
	{
		public long StartPosition;
		public FileRange.Range? Range;
		public MessagesParserFlag Flags;
		public MessagesParserDirection Direction;
		/// <summary>
		/// Message postprocess routine. Must be thread-safe.
		/// </summary>
		public Func<MessageBase, object> Postprocessor;

		public CreateParserParams(long startPosition, FileRange.Range? range = null, MessagesParserFlag flags = MessagesParserFlag.Default, MessagesParserDirection direction = MessagesParserDirection.Forward, Func<MessageBase, object> postprocessor = null)
		{
			this.StartPosition = startPosition;
			this.Range = range;
			this.Flags = flags;
			this.Direction = direction;
			this.Postprocessor = postprocessor;
		}

		public void ResetRange()
		{
			Range = null;
		}
		public void EnsureRangeIsSet(IPositionedMessagesReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (Range == null)
				Range = new FileRange.Range(reader.BeginPosition, reader.EndPosition);
		}
		public void EnsureStartPositionIsInRange()
		{
			if (Range != null)
				StartPosition = Range.Value.PutInRange(StartPosition);
		}
	};

	public struct CreateSearchingParserParams
	{
		public FileRange.Range Range;
		public SearchAllOccurencesParams SearchParams;
		public Action<long> ProgressHandler;
		public System.Threading.CancellationToken Cancellation;
		/// <summary>
		/// Message postprocess routine. Must be thread-safe.
		/// </summary>
		public Func<MessageBase, object> Postprocessor;
	};

	/// <summary>
	/// IPositionedMessagesReader is a generalization of a text log file.
	/// It represents the stream of data that support random positioning.
	/// Positions are long intergers. The stream has boundaries - BeginPosition, EndPosition.
	/// EndPosition - is a valid position but represents past-the-end position of the stream.
	/// To read messages from the stream one uses a 'parser'. Parsers created by CreateParser().
	/// </summary>
	/// <remarks>
	/// IPositionedMessagesReader introduces 'read-message-from-the-middle' problem.
	/// The problem is that a client can create a parser that starts at position that 
	/// points somewhere in the middle of a message. This reader
	/// can successfully read something that it thinks is a correct message. But it
	/// wouldn't be a correct message because it would be only half of the message 
	/// parsed by a chance. To tackle this problem the client should read at least two
	/// messages one after the other. That guarantees that the second message has 
	/// correct beginning position. Generally the client cannot be sure that 
	/// the first message starts correctly.
	/// </remarks>
	public interface IPositionedMessagesReader : IDisposable
	{
		/// <summary>
		/// Returns the minimum allowed position for this stream
		/// </summary>
		long BeginPosition { get; }
		/// <summary>
		/// Returns past-the-end position of the stream. That means that it
		/// is a valid position but there cannot be a message at this position.
		/// </summary>
		long EndPosition { get; }
		/// <summary>
		/// Updates the stream boundaries detecting them from actual media (file for instance).
		/// </summary>
		/// <param name="incrementalMode">If <value>true</value> allows the reader to optimize 
		/// the operation with assumption that the boundaries have been calculated already and
		/// need to be recalculated only if the actual media has changed.</param>
		/// <returns>Returns <value>true</value> if the boundaries have actually changed. Return value can be used for optimization.</returns>
		UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode);

		/// <summary>
		/// Returns position's distance that the reader recommends 
		/// as the radius of the range that client may read from this reader.
		/// This property defines the recommended limit of messages
		/// that could be read and kept in the memory at a time.
		/// </summary>
		long ActiveRangeRadius { get; }

		long MaximumMessageSize { get; }

		long PositionRangeToBytes(FileRange.Range range);

		long SizeInBytes { get; }

		/// <summary>
		/// Creates an object that reads messages from reader's media.
		/// </summary>
		/// <param name="startPosition">
		/// Parser starts from position defined by <paramref name="startPosition"/>.
		/// The first read message may have position bigger than <paramref name="startPosition"/>.
		/// </param>
		/// <param name="range">Defines the range of positions that the parser should stay in. 
		/// If <value>null</value> is passed then the parser is limited by reader's BeginPosition/EndPosition</param>
		/// <param name="isMainStreamReader"></param>
		/// <returns>Returns parser object. It must be disposed when is not needed.</returns>
		/// <remarks>
		/// <paramref name="startPosition"/> doesn't have to point to the beginning of a message.
		/// It is reader's responsibility to guarantee that the correct nearest message is read.
		/// </remarks>
		IPositionedMessagesParser CreateParser(CreateParserParams p);

		IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p);
	};

	public struct PostprocessedMessage
	{
		public readonly MessageBase Message;
		public readonly object PostprocessingResult;
		public PostprocessedMessage(MessageBase msg, object postprocessingResult)
		{
			Message = msg;
			PostprocessingResult = postprocessingResult;
		}
	};

	public interface IPositionedMessagesParser : IDisposable
	{
		MessageBase ReadNext();
		PostprocessedMessage ReadNextAndPostprocess();
	};
}
