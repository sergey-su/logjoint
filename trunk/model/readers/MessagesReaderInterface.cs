﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    [Flags]
    public enum UpdateBoundsStatus
    {
        NothingUpdated = 0,
        NewMessagesAvailable = 1,
        OldMessagesAreInvalid = 2,
        MessagesFiltered = 3,
    };

    public enum ReadMessagesDirection
    {
        Forward,
        Backward
    };

    [Flags]
    public enum ReadMessagesFlag
    {
        None = 0,
        Default = 0,
        HintMassiveSequentialReading = 1,
        HintMessageTimeIsNotNeeded = 2,
        HintMessageContentIsNotNeeed = 4,
        DisableDejitter = 8,
        DisableMultithreading = 16,
        DisablePlainTextSearchOptimization = 32,
    };

    public interface IMessagesPostprocessor : IDisposable
    {
        object Postprocess(IMessage message);
    };

    public struct ReadMessagesParams
    {
        /// <summary>
        /// Parser starts from position defined by StartPosition.
        /// The first read message may have position bigger than StartPosition"/>.
        /// </summary>
        public long StartPosition;
        /// <summary>
        /// Defines the range of positions that the parser should stay in. 
        /// If <value>null</value> is passed then the parser is limited by reader's BeginPosition/EndPosition
        /// </summary>
        public FileRange.Range? Range;
        public ReadMessagesFlag Flags;
        public ReadMessagesDirection Direction;
        /// <summary>
        /// Factory delegate that creates postprocessors. 
        /// Factory will be called once in each thread that the parser uses. 
        /// Factory method must be thread-safe.
        /// </summary>
        public Func<IMessagesPostprocessor> PostprocessorsFactory;
        public CancellationToken Cancellation;

        public ReadMessagesParams(
            long startPosition,
            FileRange.Range? range = null,
            ReadMessagesFlag flags = ReadMessagesFlag.Default,
            ReadMessagesDirection direction = ReadMessagesDirection.Forward,
            Func<IMessagesPostprocessor> postprocessor = null,
            CancellationToken? cancellation = null
        )
        {
            this.StartPosition = startPosition;
            this.Range = range;
            this.Flags = flags;
            this.Direction = direction;
            this.PostprocessorsFactory = postprocessor;
            this.Cancellation = cancellation.GetValueOrDefault(CancellationToken.None);
        }

        public void ResetRange()
        {
            Range = null;
        }
        public void EnsureRangeIsSet(IMessagesReader reader)
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

    public struct SearchMessagesParams
    {
        public FileRange.Range Range;
        public SearchAllOccurencesParams SearchParams;
        public Action<long> ProgressHandler;
        public CancellationToken Cancellation;
        public ReadMessagesFlag Flags;
        public object ContinuationToken;
    };

    /// <summary>
    /// IMessagesReader is a generalization of a text log file.
    /// It represents a stream of data that supports random positioning.
    /// Positions are long integers. The stream has boundaries - BeginPosition, EndPosition.
    /// EndPosition - is a valid position but represents past-the-end position of the stream.
    /// To read messages from the stream one uses a 'parser'. Parsers created by CreateParser().
    /// </summary>
    /// <remarks>
    /// IMessagesReader introduces 'read-message-from-the-middle' problem.
    /// The problem is that a client can create a parser that starts at position that 
    /// points somewhere in the middle of a message. This reader
    /// can successfully read something that it thinks is a correct message. But it
    /// wouldn't be a correct message because it would be only half of the message 
    /// parsed by a chance. To tackle this problem the client should read at least two
    /// messages one after the other. That guarantees that the second message has 
    /// correct beginning position. Generally the client cannot be sure that 
    /// the first message starts correctly.
    /// </remarks>
    public interface IMessagesReader : IDisposable
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
        Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode);

        long MaximumMessageSize { get; }

        long PositionRangeToBytes(FileRange.Range range);

        long SizeInBytes { get; }

        ITimeOffsets TimeOffsets { get; set; }

        ValueTask<int> GetContentsEtag();

        /// <summary>
        /// Reads messages from reader's media.
        /// </summary>
        /// <remarks>
        /// ReadMessagesParams.StartPosition doesn't have to point to the beginning of a message.
        /// It is reader's responsibility to guarantee that the correct nearest message is read.
        /// </remarks>
        IAsyncEnumerable<PostprocessedMessage> Read(ReadMessagesParams p);

        IAsyncEnumerable<SearchResultMessage> Search(SearchMessagesParams p);

        /// <summary>
        /// The encoding of the underlying byte stream, if it exists.
        /// </summary>
        System.Text.Encoding Encoding { get; }
    };

    public interface ITextStreamPositioningParamsProvider
    {
        TextStreamPositioningParams TextStreamPositioningParams { get; }
    };

    public struct PostprocessedMessage
    {
        public readonly IMessage Message;
        public readonly object PostprocessingResult;
        public PostprocessedMessage(IMessage msg, object postprocessingResult)
        {
            Message = msg;
            PostprocessingResult = postprocessingResult;
        }
    };

    public struct MediaBasedReaderParams
    {
        public ILogSourceThreadsInternal Threads;
        public ILogMedia Media;
        public bool QuickFormatDetectionMode;
        public string ParentLoggingPrefix;

        public MediaBasedReaderParams(ILogSourceThreadsInternal threads, ILogMedia media,
            bool quickFormatDetectionMode = false,
            string parentLoggingPrefix = null)
        {
            Threads = threads;
            Media = media;
            QuickFormatDetectionMode = quickFormatDetectionMode;
            ParentLoggingPrefix = parentLoggingPrefix;
        }
    };

    public interface IMediaBasedReaderFactory
    {
        IMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams);
    };
}
