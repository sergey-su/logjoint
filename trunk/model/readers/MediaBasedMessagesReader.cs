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

		public long CalcMaxActiveRangeSize(IGlobalSettingsAccessor settings)
		{
			long MB = 1024 * 1024;
			long sizeThreshold = settings.FileSizes.Threshold * MB;
			long partialLoadingSize = settings.FileSizes.WindowSize * MB;

			long currentSize = this.EndPosition - this.BeginPosition;

			if (currentSize < sizeThreshold)
				return currentSize;
			else
				return partialLoadingSize;
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

		public TimeSpan TimeOffset
		{
			get { return timeOffset; }
			set { timeOffset = value; }
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

			DejitteringParams? dejitteringParams = GetDejitteringParams();
			if (dejitteringParams != null && (parserParams.Flags & MessagesParserFlag.DisableDejitter) == 0)
			{
				return new DejitteringMessagesParser(
					underlyingParserParams => new Parser(this, EnsureParserRangeDoesNotExceedReadersBoundaries(underlyingParserParams)), 
						parserParams, dejitteringParams.Value);
			}
			return new Parser(this, parserParams);
		}

		public virtual IPositionedMessagesParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return null;
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

		protected static LoadedRegex CloneRegex(LoadedRegex re, ReOptions optionsToAdd = ReOptions.None)
		{
			LoadedRegex ret;
			ret.Regex = RegularExpressions.RegexUtils.CloneRegex(re.Regex, optionsToAdd);
			ret.SuffersFromPartialMatchProblem = re.SuffersFromPartialMatchProblem;
			return ret;
		}

		#endregion

		#region Implementation

		protected class Parser : IPositionedMessagesParser
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
				this.multithreadingDisabled = (p.Flags & MessagesParserFlag.DisableMultithreading) != 0
					|| reader.settingsAccessor.MultithreadedParsingDisabled;

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

				useMultithreadedStrategy = false;

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

			public IMessage ReadNext()
			{
				return Strategy.ReadNext();
			}

			public PostprocessedMessage ReadNextAndPostprocess()
			{
				return Strategy.ReadNextAndPostprocess();
			}
		};

		protected class SearchingParser : IPositionedMessagesParser
		{
			readonly MediaBasedPositionedMessagesReader owner;
			readonly CreateSearchingParserParams parserParams;
			readonly bool plainTextSearchOptimizationAllowed;
			readonly ILogSourceThreads threads;
			readonly FileRange.Range requestedRange;
			readonly ProgressAndCancellation progressAndCancellation;
			readonly FramesTracker framesTracker = new FramesTracker();
			readonly StreamTextAccess aligmentTextAccess;
			readonly MessagesSplitter aligmentSplitter;
			readonly TextMessageCapture aligmentCapture;
			readonly IPositionedMessagesParser impl;

			public SearchingParser(
				MediaBasedPositionedMessagesReader owner, 
				CreateSearchingParserParams p, 
				bool allowPlainTextSearchOptimization,
				LoadedRegex headerRe,
				ILogSourceThreads threads)
			{
				this.owner = owner;
				this.parserParams = p;
				this.plainTextSearchOptimizationAllowed = allowPlainTextSearchOptimization && ((p.Flags & MessagesParserFlag.DisablePlainTextSearchOptimization) == 0);
				this.threads = threads;
				this.requestedRange = p.Range;
				this.aligmentTextAccess = new StreamTextAccess(owner.VolatileStream, owner.StreamEncoding, owner.textStreamPositioningParams);
				this.aligmentSplitter = new MessagesSplitter(aligmentTextAccess, CloneRegex(headerRe).Regex, GetHeaderReSplitterFlags(headerRe));
				this.aligmentCapture = new TextMessageCapture();
				this.progressAndCancellation = new ProgressAndCancellation() { progressHandler = p.ProgressHandler, cancellationToken = p.Cancellation }; 
				this.impl = MessagesParserToEnumerator.EnumeratorAsParser(Enum());
			}

			public IMessage ReadNext()
			{
				return impl.ReadNext();
			}

			public PostprocessedMessage ReadNextAndPostprocess()
			{
				return impl.ReadNextAndPostprocess();
			}

			public void Dispose()
			{
				impl.Dispose();
			}

			IEnumerable<PostprocessedMessage> Enum()
			{
				using (var threadLocalDataHolder = CreateSearchThreadLocalData(parserParams.SearchParams))
				using (var threadsBulkProcessing = threads.UnderlyingThreadsContainer.StartBulkProcessing())
				{
					Func<IMessage, object> postprocessor = msg => new MessagePostprocessingResult(
						msg, threadLocalDataHolder, parserParams.Postprocessor, parserParams.SearchParams.Options.SearchInRawText);
					foreach (var currentSearchableRange in EnumSearchableRanges())
					{
						using (var parser = CreateParserForSearchableRange(currentSearchableRange, postprocessor))
						{
							for (; ; )
							{
								var tmp = parser.ReadNextAndPostprocess();
								if (tmp.Message == null)
									break;

								if (parserParams.Cancellation.IsCancellationRequested)
									yield break;

								var msg = tmp.Message;
								var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(msg);
								var msgPostprocessingResult = (MessagePostprocessingResult)tmp.PostprocessingResult;

								if (!msgPostprocessingResult.CheckedAgainstSearchCriteria)
									msgPostprocessingResult.CheckAgainstSearchCriteria(msg, threadLocalDataHolder.Value);

								progressAndCancellation.HandleMessageReadingProgress(msg.Position);

								if (!msgPostprocessingResult.PassedSearchCriteria)
									continue;

								if (!MessagePassesFilters(msg, parserParams.SearchParams, msgPostprocessingResult.FiltersPreprocessingResult, threadsBulkProcessingResult.DisplayFilterContext))
									continue;

								yield return new PostprocessedMessage(msg, msgPostprocessingResult.ExternalPostprocessingResult);

								bool missingFrameEndFound;
								framesTracker.RegisterSearchResultMessage(msg, out missingFrameEndFound);
								if (missingFrameEndFound)
									break;
							}
						}
					}
				}

				yield return new PostprocessedMessage();
			}

			bool MessagePassesFilters(IMessage msg, SearchAllOccurencesParams p, FiltersPreprocessingResult preprocResult, FilterContext filterContext)
			{
				if (p.Filters != null)
				{
					var action = p.Filters.ProcessNextMessageAndGetItsAction(msg, preprocResult, filterContext, p.Options.SearchInRawText);
					if (action == FilterAction.Exclude)
						return false;
				}
				return true;
			}

			IEnumerable<FileRange.Range> EnumSearchableRanges()
			{
				PlainTextMatcher matcher = new PlainTextMatcher(parserParams, plainTextSearchOptimizationAllowed);
				if (!matcher.PlainTextSearchOptimizationPossible)
				{
					yield return requestedRange;
					yield break;
				}
				long? skipRangesDownThisPosition = null;
				foreach (var currentRange in EnumSearchableRangesCore(matcher))
				{
					if (skipRangesDownThisPosition == null)
					{
						yield return currentRange;
					}
					else
					{
						long skipRangesDownThisPositionVal = skipRangesDownThisPosition.Value;
						if (currentRange.End < skipRangesDownThisPositionVal) // todo: < or <= ?
							continue;
						skipRangesDownThisPosition = null;
						if (currentRange.Begin < skipRangesDownThisPositionVal) // todo: < or <= ?
							yield return new FileRange.Range(skipRangesDownThisPositionVal, currentRange.End);
						else
							yield return currentRange;
					}
					if (framesTracker.ThereIsMissingFrameEnd)
					{
						framesTracker.StartLookingForMissingFrameEnd();
						yield return new FileRange.Range(currentRange.End, requestedRange.End);
						framesTracker.StopLookingForMissingFrameEnd();
						skipRangesDownThisPosition = framesTracker.LastMessagePosition;
					}
				}
			}

			IPositionedMessagesParser CreateParserForSearchableRange(
				FileRange.Range searchableRange,
				Func<IMessage, object> messagesPostprocessor)
			{
				bool disableMultithreading = false;
				return owner.CreateParser(new CreateParserParams(
					searchableRange.Begin, searchableRange,
					MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading
					| (disableMultithreading ? MessagesParserFlag.DisableMultithreading : MessagesParserFlag.None),
					MessagesParserDirection.Forward,
					messagesPostprocessor));
			}

			IEnumerable<FileRange.Range> EnumSearchableRangesCore(PlainTextMatcher matcher)
			{
				CreateSearchingParserParams p = parserParams;
				ITextAccess ta = new StreamTextAccess(owner.VolatileStream, owner.StreamEncoding, owner.textStreamPositioningParams);
				using (var tai = ta.OpenIterator(requestedRange.Begin, TextAccessDirection.Forward))
				{
					foreach (var r in IterateMatchRanges(
						IterateMatches(tai, matcher, progressAndCancellation),
						owner.textStreamPositioningParams.AlignmentBlockSize / 2 // todo: tune this parameter to find the value giving max performance
					).Select(r => PostprocessHintRange(r)))
					{
						yield return r;
					}
				}
			}

			FileRange.Range PostprocessHintRange(FileRange.Range r)
			{
				long fixedBegin = r.Begin;
				long fixedEnd = r.End;

				int? inflateRangeBy = null;
				DejitteringParams? dejitteringParams = owner.GetDejitteringParams();
				if (dejitteringParams != null && (parserParams.Flags & MessagesParserFlag.DisableDejitter) == 0)
					inflateRangeBy = dejitteringParams.Value.JitterBufferSize;

				long firstMessageEnd;
				aligmentSplitter.BeginSplittingSession(requestedRange, r.Begin, MessagesParserDirection.Forward);
				if (aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
				{
					firstMessageEnd = aligmentCapture.BeginPosition;
					if (inflateRangeBy != null)
					{
						for (int i = 0; i < inflateRangeBy.Value; ++i)
						{
							if (!aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
								break;
							firstMessageEnd = aligmentCapture.BeginPosition;
						}
					}
				}
				else
				{
					firstMessageEnd = requestedRange.End;
				}
				aligmentSplitter.EndSplittingSession();

				aligmentSplitter.BeginSplittingSession(requestedRange, firstMessageEnd, MessagesParserDirection.Backward);
				if (aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
				{
					fixedBegin = aligmentCapture.BeginPosition;
					if (inflateRangeBy != null)
					{
						for (int i = 0; i < inflateRangeBy.Value; ++i)
						{
							if (!aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
								break;
							fixedBegin = aligmentCapture.BeginPosition;
						}
					}
				}
				aligmentSplitter.EndSplittingSession();

				if (r.IsEmpty)
					fixedEnd = firstMessageEnd;

				return new FileRange.Range(fixedBegin, fixedEnd);
			}

			static ThreadLocal<SearchAllOccurencesThreadLocalData> CreateSearchThreadLocalData(SearchAllOccurencesParams searchParams)
			{
				return new ThreadLocal<SearchAllOccurencesThreadLocalData>(() =>
					new SearchAllOccurencesThreadLocalData()
					{
						Options = searchParams.Options.Preprocess(),
						State = new Search.BulkSearchState(),
						Filters = searchParams.Filters != null ? searchParams.Filters.Clone() : null
					}
				);
			}

			static IEnumerable<long> IterateMatches(ITextAccessIterator tai, PlainTextMatcher matcher, ProgressAndCancellation progressAndCancellation)
			{
				for (; ; )
				{
					StringSlice buf = new StringSlice(tai.CurrentBuffer);
					for (int startIdx = 0; ; )
					{
						var match = matcher.Match(buf, startIdx);
						if (!match.HasValue)
							break;
						yield return tai.CharIndexToPosition(match.Value.MatchBegin);
						startIdx = match.Value.MatchEnd;
					}
					if (!tai.Advance(tai.CurrentBuffer.Length - matcher.MaxMatchLength))
					{
						break;
					}
					progressAndCancellation.HandleTextIterationProgress(tai);
					if (!progressAndCancellation.CheckTextIterationCancellation())
					{
						break;
					}
				}
			}

			static IEnumerable<FileRange.Range> IterateMatchRanges(IEnumerable<long> matches, long threshhold)
			{
				FileRange.Range? lastMatch = null;
				foreach (long match in matches)
				{
					if (lastMatch == null)
					{
						lastMatch = new FileRange.Range(match, match);
					}
					else
					{
						FileRange.Range lastMatchVal = lastMatch.Value;
						if (match - lastMatchVal.End < threshhold)
						{
							lastMatch = new FileRange.Range(lastMatchVal.Begin, match);
						}
						else
						{
							yield return lastMatchVal;
							lastMatch = new FileRange.Range(match, match);
						}
					}
				}
				if (lastMatch != null)
				{
					yield return lastMatch.Value;
				}
			}

			class MessagePostprocessingResult
			{
				public bool CheckedAgainstSearchCriteria;
				public bool PassedSearchCriteria;
				public FiltersPreprocessingResult FiltersPreprocessingResult;
				public object ExternalPostprocessingResult;
				public MessagePostprocessingResult(
					IMessage msg,
					ThreadLocal<SearchAllOccurencesThreadLocalData> dataHolder,
					Func<IMessage, object> externalPostprocessor,
					bool searchRaw)
				{
					var data = dataHolder.Value;
					if (msg != null)
					{
						if ((msg.Flags & MessageFlag.EndFrame) == 0)
							CheckAgainstSearchCriteria(msg, data);
						if (PassedSearchCriteria || !CheckedAgainstSearchCriteria)
						{
							if (data.Filters != null)
								this.FiltersPreprocessingResult = data.Filters.PreprocessMessage(msg, searchRaw);
							this.ExternalPostprocessingResult = externalPostprocessor != null ? externalPostprocessor(msg) : null;
						}
					}
				}
				public void CheckAgainstSearchCriteria(IMessage msg, SearchAllOccurencesThreadLocalData data)
				{
					this.PassedSearchCriteria = LogJoint.Search.SearchInMessageText(msg, data.Options, data.State).HasValue;
					this.CheckedAgainstSearchCriteria = true;
				}
			};

			class SearchAllOccurencesThreadLocalData
			{
				public Search.PreprocessedOptions Options;
				public Search.BulkSearchState State;
				public IFiltersList Filters;
			};

			class FramesTracker
			{
				public bool ThereIsMissingFrameEnd { get { return frameLevel > 0; } }

				public void RegisterSearchResultMessage(IMessage msg, out bool missingFrameEndFound)
				{
					missingFrameEndFound = false;
					if (msg == null)
						return;
					MessageFlag flags = msg.Flags;
					switch (flags & MessageFlag.TypeMask)
					{
						case MessageFlag.StartFrame:
							++frameLevel;
							break;
						case MessageFlag.EndFrame:
							if (ThereIsMissingFrameEnd)
							{
								--frameLevel;
								if (lookingForFrameEnd && frameLevel == 0)
									missingFrameEndFound = true;
							}
							break;
					}
					lastMessagePosition = msg.Position;
				}

				public void StartLookingForMissingFrameEnd()
				{
					if (lookingForFrameEnd)
						throw new InvalidOperationException("Already looking for missing frame end");
					lookingForFrameEnd = true;
				}

				public void StopLookingForMissingFrameEnd()
				{
					if (!lookingForFrameEnd)
						throw new InvalidOperationException("Not looking for missing frame end");
					lookingForFrameEnd = false;
				}

				public long LastMessagePosition { get { return lastMessagePosition; } }

				int frameLevel;
				bool lookingForFrameEnd;
				long lastMessagePosition;
			};

			class PlainTextMatcher
			{
				public PlainTextMatcher(CreateSearchingParserParams p, bool plainTextSearchOptimizationAllowed)
				{
					plainTextSearchOptimizationPossible = true;

					if (!plainTextSearchOptimizationAllowed)
					{
						plainTextSearchOptimizationPossible = false;
					}
					else if (p.SearchParams.Options.Template.Length == 0)
					{
						plainTextSearchOptimizationPossible = false;
					}
					else if (p.SearchParams.Filters != null && 
						p.SearchParams.Filters.FilteringEnabled &&
						p.SearchParams.Filters.Items.FirstOrDefault(f => f.Enabled && f.MatchFrameContent) != null)
					{
						plainTextSearchOptimizationPossible = false;
					}
					else if (p.SearchParams.Options.Regexp) // todo: detect and handle fixed-length regexps
					{
						plainTextSearchOptimizationPossible = false;
					}
					else
					{
						maxMatchLength = p.SearchParams.Options.Template.Length;
					}
					if (plainTextSearchOptimizationPossible)
					{
						var fixedOptions = p.SearchParams.Options;
						fixedOptions.ReverseSearch = false;
						opts = fixedOptions.Preprocess();
						searchState = new Search.BulkSearchState();
					}
				}

				public bool PlainTextSearchOptimizationPossible { get { return plainTextSearchOptimizationPossible; } }

				public int MaxMatchLength { get { return maxMatchLength; } }

				public Search.MatchedTextRange? Match(StringSlice s, int startIndex)
				{
					return Search.SearchInText(s, opts, searchState, startIndex);
				}

				bool plainTextSearchOptimizationPossible;
				int maxMatchLength;
				Search.PreprocessedOptions opts;
				Search.BulkSearchState searchState;
			};

			class ProgressAndCancellation
			{
				public Action<long> progressHandler;
				public CancellationToken cancellationToken;
				public int blocksReadSinseLastProgressUpdate;
				public int lastTimeHandlerWasCalled;
				public int blocksReadSinseLastCancellationCheck;
				public int messagesReadSinseLastProgressUpdate;

				public void HandleTextIterationProgress(ITextAccessIterator tai)
				{
					int checkProgressConditionEvery = 64;
					if (progressHandler != null)
					{
						if ((++blocksReadSinseLastProgressUpdate % checkProgressConditionEvery) == 0)
						{
							int now = Environment.TickCount;
							if (now - lastTimeHandlerWasCalled > 1000)
							{
								progressHandler(tai.CharIndexToPosition(0));
								lastTimeHandlerWasCalled = now;
								blocksReadSinseLastProgressUpdate = 0;
								messagesReadSinseLastProgressUpdate = 0;
							}
						}
					}
				}

				public bool CheckTextIterationCancellation()
				{
					int checkCancellationConditionEvery = 16;
					if ((++blocksReadSinseLastCancellationCheck % checkCancellationConditionEvery) == 0)
					{
						blocksReadSinseLastCancellationCheck = 0;
						if (cancellationToken.IsCancellationRequested)
							return false;
					}
					return true;
				}

				public void HandleMessageReadingProgress(long lastReadPosition)
				{
					int checkProgressConditionEvery = 1024;
					if (progressHandler != null)
					{
						if ((++messagesReadSinseLastProgressUpdate % checkProgressConditionEvery) == 0)
						{
							int now = Environment.TickCount;
							if (now - lastTimeHandlerWasCalled > 1000)
							{
								progressHandler(lastReadPosition);
								lastTimeHandlerWasCalled = now;
								blocksReadSinseLastProgressUpdate = 0;
								messagesReadSinseLastProgressUpdate = 0;
							}
						}
					}
				}
			};
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
		TimeSpan timeOffset;
		#endregion
	};

	internal class MessagesBuilderCallback : IMessagesBuilderCallback
	{
		readonly ILogSourceThreads threads;
		readonly IThread fakeThread;
		long currentPosition;

		public MessagesBuilderCallback(ILogSourceThreads threads, IThread fakeThread)
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
