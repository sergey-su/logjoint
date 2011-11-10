using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using LogJoint.StreamParsingStrategies;

namespace LogJoint
{
	/// <summary>
	/// Implements IPositionedMessagesReader interface by getting the data from ILogMedia object.
	/// </summary>
	public abstract class MediaBasedPositionedMessagesReader : IPositionedMessagesReader
	{
		internal MediaBasedPositionedMessagesReader(
			ILogMedia media,
			BoundFinder beginFinder,
			BoundFinder endFinder,
			MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
			TextStreamPositioningParams textStreamPositioningParams
		)
		{
			this.beginFinder = beginFinder;
			this.endFinder = endFinder;
			this.media = media;
			this.textStreamPositioningParams = textStreamPositioningParams;
			this.singleThreadedStrategy = new Lazy<BaseStrategy>(CreateSingleThreadedStrategy);
			this.multiThreadedStrategy = new Lazy<BaseStrategy>(CreateMultiThreadedStrategy);
			this.extensions = new MessagesReaderExtensions(this, extensionsInitData);
		}

		#region IPositionedMessagesReader

		public long BeginPosition
		{
			get
			{
				return beginPosition.Value;
			}
		}

		public long EndPosition
		{
			get
			{
				return endPosition.Value;
			}
		}

		public long ActiveRangeRadius
		{
			get { return 1024 * 1024 * 4; }
		}

		public long MaximumMessageSize
		{
			get { return textStreamPositioningParams.AlignmentBlockSize; }
		}

		public long PositionRangeToBytes(FileRange.Range range)
		{
			// Here calculation is not precise: TextStreamPosition cannot be converted to bytes 
			// directly and efficiently. But this function is used only for statistics so it's ok to 
			// use approximate calculations here.
			var encoding = StreamEncoding;
			return TextStreamPositionToStreamPosition_Approx(range.End, encoding, textStreamPositioningParams) - TextStreamPositionToStreamPosition_Approx(range.Begin, encoding, textStreamPositioningParams);
		}

		public long SizeInBytes
		{
			get { return mediaSize; }
		}

		public UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode)
		{
			var ret = UpdateAvailableBoundsInternal(ref incrementalMode);
			Extensions.NotifyExtensionsAboutUpdatedAvailableBounds(incrementalMode, ret);
			return ret;
		}

		public IPositionedMessagesParser CreateParser(CreateParserParams parserParams)
		{
			parserParams.EnsureRangeIsSet(this);

			DejitteringParams? dejitteringParams = GetDejitteringParams();
			if (dejitteringParams != null && (parserParams.Flags & MessagesParserFlag.DisableDejitter) == 0)
			{
				return new DejitteringMessagesParser(
					underlyingParserParams => new Parser(this, EnsureParserRangeDoesNotExceedReadersBoundaries(underlyingParserParams)), 
						parserParams, dejitteringParams.Value);
			}
			return new Parser(this, parserParams);
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
		}

		#endregion

		#region Members to be overriden in child class

		protected abstract Encoding DetectStreamEncoding(Stream stream);

		protected abstract BaseStrategy CreateSingleThreadedStrategy();
		protected abstract BaseStrategy CreateMultiThreadedStrategy();

		protected virtual DejitteringParams? GetDejitteringParams()
		{
			return null;
		}

		#endregion

		#region Public interface

		public ILogMedia LogMedia
		{
			get { return media; }
		}

		public Stream VolatileStream
		{
			get { return media.DataStream; }
		}

		public void EnsureStreamEncodingIsCached()
		{
			if (encoding == null)
				encoding = DetectStreamEncoding(media.DataStream);
		}

		public Encoding StreamEncoding
		{
			get
			{
				EnsureStreamEncodingIsCached();
				return encoding;
			}
		}

		#endregion

		#region Protected interface

		protected DateTime MediaLastModified
		{
			get { return media.LastModified; }
		}

		protected MessagesReaderExtensions Extensions
		{
			get
			{
				return extensions;
			}
		}

		protected static MessagesSplitterFlags GetHeaderReSplitterFlags(LoadedRegex headerRe)
		{
			MessagesSplitterFlags ret = MessagesSplitterFlags.None;
			if (headerRe.SuffersFromPartialMatchProblem)
				ret |= MessagesSplitterFlags.PreventBufferUnderflow;
			return ret;
		}

		protected static MakeMessageFlags ParserFlagsToMakeMessageFlags(MessagesParserFlag flags)
		{
			MakeMessageFlags ret = MakeMessageFlags.Default;
			if ((flags & MessagesParserFlag.HintMessageTimeIsNotNeeded) != 0)
				ret |= MakeMessageFlags.HintIgnoreTime;
			if ((flags & MessagesParserFlag.HintMessageContentIsNotNeeed) != 0)
				ret |= (MakeMessageFlags.HintIgnoreBody | MakeMessageFlags.HintIgnoreEntryType
					| MakeMessageFlags.HintIgnoreSeverity | MakeMessageFlags.HintIgnoreThread);
			return ret;
		}


		protected static LoadedRegex CloneRegex(LoadedRegex re)
		{
			LoadedRegex ret;
			if (re.Regex != null)
				ret.Regex = re.Regex.Factory.Create(re.Regex.Pattern, re.Regex.Options);
			else
				ret.Regex = null;
			ret.SuffersFromPartialMatchProblem = re.SuffersFromPartialMatchProblem;
			return ret;
		}

		#endregion

		#region Implementation

		class Parser : IPositionedMessagesParser
		{
			private bool disposed;
			private readonly bool isSequentialReadingParser;
			private readonly bool multithreadingDisabled;
			protected readonly CreateParserParams InitialParams;
			protected readonly MediaBasedPositionedMessagesReader Reader;
			protected readonly StreamParsingStrategies.BaseStrategy Strategy;

			public Parser(MediaBasedPositionedMessagesReader reader, CreateParserParams p)
			{
				p.EnsureRangeIsSet(reader);

				this.Reader = reader;
				this.InitialParams = p;

				this.isSequentialReadingParser = (p.Flags & MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading) != 0;
				this.multithreadingDisabled = (p.Flags & MessagesParserFlag.DisableMultithreading) != 0;

				CreateParsingStrategy(reader, p, out this.Strategy);
				
				this.Strategy.ParserCreated(p);
			}

			static bool HeuristicallyDetectWhetherMultithreadingMakesSense(CreateParserParams parserParams,
				TextStreamPositioningParams textStreamPositioningParams)
			{
#if SILVERLIGHT
				return false;
#else
				if (System.Environment.ProcessorCount == 1)
				{
					return false;
				}

				long approxBytesToRead;
				if (parserParams.Direction == MessagesParserDirection.Forward)
				{
					approxBytesToRead = new TextStreamPosition(parserParams.Range.Value.End, textStreamPositioningParams).StreamPositionAlignedToBlockSize
						- new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
				}
				else
				{
					approxBytesToRead = new TextStreamPosition(parserParams.StartPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize
						- new TextStreamPosition(parserParams.Range.Value.Begin, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
				}
				if (approxBytesToRead < MultiThreadedStrategy<int>.GetBytesToParsePerThread(textStreamPositioningParams) * 2)
				{
					return false;
				}

				return true;
#endif
			}

			void CreateParsingStrategy(MediaBasedPositionedMessagesReader reader, CreateParserParams parserParams, out BaseStrategy strategy)
			{
				bool useMultithreadedStrategy;
				
				if (multithreadingDisabled)
					useMultithreadedStrategy = false;
				else if (!isSequentialReadingParser)
					useMultithreadedStrategy = false;
				else
					useMultithreadedStrategy = HeuristicallyDetectWhetherMultithreadingMakesSense(parserParams, reader.textStreamPositioningParams);

				//useMultithreadedStrategy = false;

				Lazy<BaseStrategy> strategyToTryFirst;
				Lazy<BaseStrategy> strategyToTrySecond;
				if (useMultithreadedStrategy)
				{
					strategyToTryFirst = reader.multiThreadedStrategy;
					strategyToTrySecond = reader.singleThreadedStrategy;
				}
				else
				{
					strategyToTryFirst = reader.singleThreadedStrategy;
					strategyToTrySecond = reader.multiThreadedStrategy;
				}

				strategy = strategyToTryFirst.Value;
				if (strategy == null)
					strategy = strategyToTrySecond.Value;
			}

			public bool IsDisposed
			{
				get { return disposed; }
			}

			public virtual void Dispose()
			{
				if (disposed)
					return;
				disposed = true;
				Strategy.ParserDestroyed();
			}

			public MessageBase ReadNext()
			{
				return Strategy.ReadNext();
			}
		};

		private bool UpdateMediaSize()
		{
			long tmp = media.Size;
			if (tmp == mediaSize)
				return false;
			mediaSize = tmp;
			return true;
		}

		private static TextStreamPosition FindBound(BoundFinder finder, Stream stm, Encoding encoding, string boundName,
			TextStreamPositioningParams textStreamPositioningParams)
		{
			TextStreamPosition? pos = finder.Find(stm, encoding, textStreamPositioningParams);
			if (!pos.HasValue)
				throw new Exception(string.Format("Cannot detect the {0} of the log", boundName));
			return pos.Value;
		}

		private TextStreamPosition DetectEndPositionFromMediaSize()
		{
			return StreamTextAccess.StreamPositionToTextStreamPosition(mediaSize, StreamEncoding, VolatileStream, textStreamPositioningParams);
		}

		private void FindLogicalBounds(bool incrementalMode)
		{
			TextStreamPosition defaultBegin = new TextStreamPosition(0, TextStreamPosition.AlignMode.BeginningOfContainingBlock, textStreamPositioningParams);
			TextStreamPosition defaultEnd = DetectEndPositionFromMediaSize();

			TextStreamPosition newBegin = incrementalMode ? beginPosition : defaultBegin;
			TextStreamPosition newEnd = defaultEnd;

			beginPosition = defaultBegin;
			endPosition = defaultEnd;
			try
			{
				if (!incrementalMode && beginFinder != null)
				{
					newBegin = FindBound(beginFinder, VolatileStream, StreamEncoding, "beginning", textStreamPositioningParams);
				}
				if (endFinder != null)
				{
					newEnd = FindBound(endFinder, VolatileStream, StreamEncoding, "end", textStreamPositioningParams);
				}
			}
			finally
			{
				beginPosition = newBegin;
				endPosition = newEnd;
			}
		}

		UpdateBoundsStatus UpdateAvailableBoundsInternal(ref bool incrementalMode)
		{
			media.Update();

			// Save the current physical stream end
			long prevMediaSize = mediaSize;

			// Reread the physical stream end
			if (!UpdateMediaSize())
			{
				// The stream has the same size as it had before
				return UpdateBoundsStatus.NothingUpdated;
			}

			bool oldMessagesAreInvalid = false;

			if (mediaSize < prevMediaSize)
			{
				// The size of source file has reduced. This means that the 
				// file was probably overwritten. We have to delete all the messages 
				// we have loaded so far and start loading the file from the beginning.
				// Otherwise there is a high possibility of messages' integrity violation.
				// Fall to non-incremental mode
				incrementalMode = false;
				oldMessagesAreInvalid = true;
			}

			FindLogicalBounds(incrementalMode);

			if (oldMessagesAreInvalid)
				return UpdateBoundsStatus.OldMessagesAreInvalid;

			return UpdateBoundsStatus.NewMessagesAvailable;
		}

		private CreateParserParams EnsureParserRangeDoesNotExceedReadersBoundaries(CreateParserParams p)
		{
			if (p.Range != null)
				p.Range = FileRange.Range.Intersect(p.Range.Value,
					new FileRange.Range(BeginPosition, EndPosition)).Common;
			return p;
		}

		private static long TextStreamPositionToStreamPosition_Approx(long pos, Encoding encoding, TextStreamPositioningParams positioningParams)
		{
			TextStreamPosition txtPos = new TextStreamPosition(pos, positioningParams);
			return txtPos.StreamPositionAlignedToBlockSize + encoding.GetMaxByteCount(txtPos.CharPositionInsideBuffer);
		}
		
		readonly ILogMedia media;
		readonly BoundFinder beginFinder;
		readonly BoundFinder endFinder;
		readonly MessagesReaderExtensions extensions;
		readonly Lazy<StreamParsingStrategies.BaseStrategy> singleThreadedStrategy;
		readonly Lazy<StreamParsingStrategies.BaseStrategy> multiThreadedStrategy;
		readonly TextStreamPositioningParams textStreamPositioningParams;

		Encoding encoding;

		long mediaSize;
		TextStreamPosition beginPosition;
		TextStreamPosition endPosition;
		#endregion
	};

	internal class MessagesBuilderCallback : IMessagesBuilderCallback
	{
		readonly LogSourceThreads threads;
		readonly IThread fakeThread;
		long currentPosition;

		public MessagesBuilderCallback(LogSourceThreads threads, IThread fakeThread)
		{
			this.threads = threads;
			this.fakeThread = fakeThread;
		}

		public long CurrentPosition
		{
			get { return currentPosition; }
		}

		public IThread GetThread(StringSlice id)
		{
			return fakeThread ?? threads.GetThread(id);
		}

		internal void SetCurrentPosition(long value)
		{
			currentPosition = value;
		}
	};	
}
