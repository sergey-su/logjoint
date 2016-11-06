using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Linq;
using LogJoint.StreamParsingStrategies;
using LogJoint.Settings;
using LogJoint.RegularExpressions;
using System.Security.Cryptography;

namespace LogJoint
{
	/// <summary>
	/// Implements IPositionedMessagesReader interface by getting the data from ILogMedia object.
	/// </summary>
	public abstract class MediaBasedPositionedMessagesReader : IPositionedMessagesReader, ITextStreamPositioningParamsProvider
	{
		internal MediaBasedPositionedMessagesReader(
			ILogMedia media,
			BoundFinder beginFinder,
			BoundFinder endFinder,
			MessagesReaderExtensions.XmlInitializationParams extensionsInitData,
			TextStreamPositioningParams textStreamPositioningParams,
			MessagesReaderFlags flags,
			Settings.IGlobalSettingsAccessor settingsAccessor
		)
		{
			this.beginFinder = beginFinder;
			this.endFinder = endFinder;
			this.media = media;
			this.textStreamPositioningParams = textStreamPositioningParams;
			this.singleThreadedStrategy = new Lazy<BaseStrategy>(CreateSingleThreadedStrategy);
			this.multiThreadedStrategy = new Lazy<BaseStrategy>(CreateMultiThreadedStrategy);
			this.extensions = new MessagesReaderExtensions(this, extensionsInitData);
			this.flags = flags;
			this.settingsAccessor = settingsAccessor;
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

		public ITimeOffsets TimeOffsets
		{
			get { return timeOffsets; }
			set { timeOffsets = value; }
		}

		public UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode)
		{
			var ret = UpdateAvailableBoundsInternal(ref incrementalMode);
			Extensions.NotifyExtensionsAboutUpdatedAvailableBounds(new AvailableBoundsUpdateNotificationArgs()
			{
				Status = ret,
				IsIncrementalMode = incrementalMode,
				IsQuickFormatDetectionMode = this.IsQuickFormatDetectionMode
			});
			return ret;
		}

		public IPositionedMessagesParser CreateParser(CreateParserParams parserParams)
		{
			parserParams.EnsureRangeIsSet(this);

			var strategiesCache = new Parser.StrategiesCache()
			{
				MultiThreadedStrategy = multiThreadedStrategy,
				SingleThreadedStrategy = singleThreadedStrategy
			};

			DejitteringParams? dejitteringParams = GetDejitteringParams();
			if (dejitteringParams != null && (parserParams.Flags & MessagesParserFlag.DisableDejitter) == 0)
			{
				return new DejitteringMessagesParser(
					underlyingParserParams => new Parser(
						this,
						EnsureParserRangeDoesNotExceedReadersBoundaries(underlyingParserParams),
						textStreamPositioningParams,
						settingsAccessor,
						strategiesCache
					),  parserParams,  dejitteringParams.Value);
			}
			return new Parser(
				this, 
				parserParams,
				textStreamPositioningParams,
				settingsAccessor,
				strategiesCache
			);
		}

		public virtual IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return null;
		}

		int IPositionedMessagesReader.GetContentsEtag()
		{
			VolatileStream.Position = 0;
			byte[] buf = new byte[1024];
			int read = VolatileStream.Read(buf, 0, buf.Length);
			return Hashing.GetStableHashCode(buf, 0, read);
		}

		public MessagesReaderFlags Flags
		{
			get { return flags; }
		}

		public bool IsQuickFormatDetectionMode
		{
			get { return (flags & MessagesReaderFlags.QuickFormatDetectionMode) != 0; }
		}

		#endregion

		#region ITextStreamPositioningParamsProvider

		TextStreamPositioningParams ITextStreamPositioningParamsProvider.TextStreamPositioningParams { get { return textStreamPositioningParams; } }

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

		protected static LoadedRegex CloneRegex(LoadedRegex re, ReOptions optionsToAdd = ReOptions.None)
		{
			return re.Clone(optionsToAdd);
		}

		#endregion

		#region Implementation

		protected class Parser : IPositionedMessagesParser
		{
			private bool disposed;
			private readonly bool isSequentialReadingParser;
			private readonly bool multithreadingDisabled;
			protected readonly CreateParserParams InitialParams;
			protected readonly StreamParsingStrategies.BaseStrategy Strategy;

			public Parser(
				IPositionedMessagesReader owner, 
				CreateParserParams p,
				TextStreamPositioningParams textStreamPositioningParams,
				IGlobalSettingsAccessor globalSettings,
				StrategiesCache strategiesCache
			)
			{
				p.EnsureRangeIsSet(owner);

				this.InitialParams = p;

				this.isSequentialReadingParser = (p.Flags & MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading) != 0;
				this.multithreadingDisabled = (p.Flags & MessagesParserFlag.DisableMultithreading) != 0
					|| globalSettings.MultithreadedParsingDisabled;

				CreateParsingStrategy(p, textStreamPositioningParams, strategiesCache, out this.Strategy);
				
				this.Strategy.ParserCreated(p);
			}

			public struct StrategiesCache
			{
				public Lazy<BaseStrategy> MultiThreadedStrategy;
				public Lazy<BaseStrategy> SingleThreadedStrategy;
			};

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

			void CreateParsingStrategy(
				CreateParserParams parserParams,
				TextStreamPositioningParams textStreamPositioningParams,
				StrategiesCache strategiesCache,
				out BaseStrategy strategy)
			{
				bool useMultithreadedStrategy;
				
				if (multithreadingDisabled)
					useMultithreadedStrategy = false;
				else if (!isSequentialReadingParser)
					useMultithreadedStrategy = false;
				else
					useMultithreadedStrategy = HeuristicallyDetectWhetherMultithreadingMakesSense(parserParams, textStreamPositioningParams);

				useMultithreadedStrategy = false;

				Lazy<BaseStrategy> strategyToTryFirst;
				Lazy<BaseStrategy> strategyToTrySecond;
				if (useMultithreadedStrategy)
				{
					strategyToTryFirst = strategiesCache.MultiThreadedStrategy;
					strategyToTrySecond = strategiesCache.SingleThreadedStrategy;
				}
				else
				{
					strategyToTryFirst = strategiesCache.SingleThreadedStrategy;
					strategyToTrySecond = strategiesCache.MultiThreadedStrategy;
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

			public IMessage ReadNext()
			{
				return Strategy.ReadNext();
			}

			public PostprocessedMessage ReadNextAndPostprocess()
			{
				return Strategy.ReadNextAndPostprocess();
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
			int byteCount;
			if (encoding == Encoding.UTF8)
				byteCount = txtPos.CharPositionInsideBuffer; // usually utf8 use latin chars. 1 char -> 1 byte.
			else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
				byteCount = txtPos.CharPositionInsideBuffer * 2; // usually UTF16 does not user surrogates. 1 char -> 2 bytes.
			else
				byteCount = encoding.GetMaxByteCount(txtPos.CharPositionInsideBuffer); // default formula
			return txtPos.StreamPositionAlignedToBlockSize + byteCount;
		}
		
		readonly ILogMedia media;
		readonly BoundFinder beginFinder;
		readonly BoundFinder endFinder;
		readonly MessagesReaderExtensions extensions;
		readonly Lazy<StreamParsingStrategies.BaseStrategy> singleThreadedStrategy;
		readonly Lazy<StreamParsingStrategies.BaseStrategy> multiThreadedStrategy;
		readonly TextStreamPositioningParams textStreamPositioningParams;
		readonly MessagesReaderFlags flags;
		readonly Settings.IGlobalSettingsAccessor settingsAccessor;

		Encoding encoding;

		long mediaSize;
		TextStreamPosition beginPosition;
		TextStreamPosition endPosition;
		ITimeOffsets timeOffsets = LogJoint.TimeOffsets.Empty;
		#endregion
	};

	internal class MessagesBuilderCallback : IMessagesBuilderCallback
	{
		readonly ILogSourceThreads threads;
		readonly IThread fakeThread;
		long currentBeginPosition, currentEndPosition;

		public MessagesBuilderCallback(ILogSourceThreads threads, IThread fakeThread)
		{
			this.threads = threads;
			this.fakeThread = fakeThread;
		}

		public long CurrentPosition
		{
			get { return currentBeginPosition; }
		}

		public long CurrentEndPosition
		{
			get { return currentEndPosition; }
		}

		public IThread GetThread(StringSlice id)
		{
			return fakeThread ?? threads.GetThread(id);
		}

		internal void SetCurrentPosition(long beginPosition, long endPosition)
		{
			currentBeginPosition = beginPosition;
			currentEndPosition = endPosition;
		}
	};	
}
