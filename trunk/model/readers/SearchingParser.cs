using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint
{
	class SearchingParser : IPositionedMessagesParser
	{
		readonly IPositionedMessagesReader owner;
		readonly CreateSearchingParserParams parserParams;
		readonly DejitteringParams? dejitteringParams;
		readonly TextStreamPositioningParams textStreamPositioningParams;
		readonly Stream rawStream;
		readonly Encoding streamEncoding;
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
			IPositionedMessagesReader owner,
			CreateSearchingParserParams p,
			TextStreamPositioningParams textStreamPositioningParams,
			DejitteringParams? dejitteringParams,
			Stream rawStream,
			Encoding streamEncoding,
			bool allowPlainTextSearchOptimization,
			LoadedRegex headerRe,
			ILogSourceThreads threads)
		{
			this.owner = owner;
			this.parserParams = p;
			this.plainTextSearchOptimizationAllowed = allowPlainTextSearchOptimization && ((p.Flags & MessagesParserFlag.DisablePlainTextSearchOptimization) == 0);
			this.threads = threads;
			this.requestedRange = p.Range;
			this.textStreamPositioningParams = textStreamPositioningParams;
			this.dejitteringParams = dejitteringParams;
			this.rawStream = rawStream;
			this.streamEncoding = streamEncoding;
			var continuationToken = p.ContinuationToken as ContinuationToken;
			if (continuationToken != null)
				this.requestedRange = new FileRange.Range(continuationToken.NextPosition, requestedRange.End);
			this.aligmentTextAccess = new StreamTextAccess(rawStream, streamEncoding, textStreamPositioningParams);
			this.aligmentSplitter = new MessagesSplitter(aligmentTextAccess, headerRe.Clone().Regex, headerRe.GetHeaderReSplitterFlags());
			this.aligmentCapture = new TextMessageCapture();
			this.progressAndCancellation = new ProgressAndCancellation()
			{
				progressHandler = p.ProgressHandler,
				cancellationToken = p.Cancellation,
				continuationToken = new ContinuationToken() { NextPosition = requestedRange.Begin }
			};
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
					msg, threadLocalDataHolder, parserParams.Postprocessor, parserParams.SearchParams.SearchInRawText);
				foreach (var currentSearchableRange in EnumSearchableRanges())
				{
					using (var parser = CreateParserForSearchableRange(currentSearchableRange, postprocessor))
					{
						for (;;)
						{
							var tmp = parser.ReadNextAndPostprocess();
							if (tmp.Message == null)
								break;

							var msg = tmp.Message;
							var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(msg);
							var msgPostprocessingResult = (MessagePostprocessingResult)tmp.PostprocessingResult;

							if (!msgPostprocessingResult.CheckedAgainstSearchCriteria)
								msgPostprocessingResult.CheckAgainstSearchCriteria(msg, threadLocalDataHolder.Value);

							progressAndCancellation.HandleMessageReadingProgress(msg.Position);

							if (msgPostprocessingResult.PassedSearchCriteria)
							{
								yield return new PostprocessedMessage(msg, msgPostprocessingResult.ExternalPostprocessingResult);

								bool missingFrameEndFound;
								framesTracker.RegisterSearchResultMessage(msg, out missingFrameEndFound);
								if (missingFrameEndFound)
									break;
							}

							progressAndCancellation.continuationToken.NextPosition = msg.EndPosition;
							progressAndCancellation.CheckTextIterationCancellation();
						}
					}
				}
			}

			yield return new PostprocessedMessage();
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
			bool disableMultithreading = true;
			return owner.CreateParser(new CreateParserParams(
				searchableRange.Begin, searchableRange,
				MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading
				| (disableMultithreading ? MessagesParserFlag.DisableMultithreading : MessagesParserFlag.None),
				MessagesParserDirection.Forward,
				messagesPostprocessor));
		}

		IEnumerable<FileRange.Range> EnumSearchableRangesCore(PlainTextMatcher matcher)
		{
			ITextAccess ta = new StreamTextAccess(rawStream, streamEncoding, textStreamPositioningParams);
			using (var tai = ta.OpenIterator(requestedRange.Begin, TextAccessDirection.Forward))
			{
				var lastRange = new FileRange.Range();
				foreach (var r in
					IterateMatchRanges(
						EnumCheckpoints(tai, matcher, progressAndCancellation),
						textStreamPositioningParams.AlignmentBlockSize / 2, // todo: tune this parameter to find the value giving max performance
						progressAndCancellation
					)
					.Select(r => PostprocessHintRange(r, lastRange))
				)
				{
					lastRange = r;
					yield return r;
				}
			}
		}

		FileRange.Range PostprocessHintRange(FileRange.Range r, FileRange.Range lastRange)
		{
			long fixedBegin = r.Begin;
			long fixedEnd = r.End;

			int? inflateRangeBy = null;
			if (dejitteringParams != null && (parserParams.Flags & MessagesParserFlag.DisableDejitter) == 0)
				inflateRangeBy = dejitteringParams.Value.JitterBufferSize;

			aligmentSplitter.BeginSplittingSession(requestedRange, r.End, MessagesParserDirection.Forward);
			if (aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
			{
				fixedEnd = aligmentCapture.EndPosition;
				if (inflateRangeBy != null)
				{
					for (int i = 0; i < inflateRangeBy.Value; ++i)
					{
						if (!aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
							break;
						fixedEnd = aligmentCapture.EndPosition;
					}
				}
			}
			else
			{
				fixedEnd = requestedRange.End;
			}
			aligmentSplitter.EndSplittingSession();

			aligmentSplitter.BeginSplittingSession(requestedRange, fixedBegin, MessagesParserDirection.Backward);
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

			var ret = new FileRange.Range(fixedBegin, fixedEnd);
			ret = FileRange.Range.Intersect(ret, requestedRange).Common;
			var lastRangeIntersection = FileRange.Range.Intersect(ret, lastRange);
			if (lastRangeIntersection.RelativePosition == 0)
				ret = lastRangeIntersection.Leftover1Right;

			return ret;
		}

		static ThreadLocal<SearchAllOccurencesThreadLocalData> CreateSearchThreadLocalData(SearchAllOccurencesParams searchParams)
		{
			return new ThreadLocal<SearchAllOccurencesThreadLocalData>(() =>
				new SearchAllOccurencesThreadLocalData()
				{
					BulkProcessing = searchParams.Filters.StartBulkProcessing(searchParams.SearchInRawText),
				}
			);
		}


		struct Checkpoint
		{
			public long Position, EndPosition;
			public bool IsMatch;
		};

		static IEnumerable<Checkpoint> EnumCheckpoints(
			ITextAccessIterator tai, PlainTextMatcher matcher, ProgressAndCancellation progressAndCancellation)
		{
			for (;;)
			{
				StringSlice buf = new StringSlice(tai.CurrentBuffer);
				for (int startIdx = 0; ;)
				{
					var match = matcher.Match(buf, startIdx);
					if (!match.HasValue)
						break;
					yield return new Checkpoint()
					{
						Position = tai.CharIndexToPosition(match.Value.MatchBegin),
						EndPosition = tai.CharIndexToPosition(match.Value.MatchEnd),
						IsMatch = true
					};
					startIdx = match.Value.MatchEnd;
					progressAndCancellation.CheckTextIterationCancellation();
				}
				if (tai.CurrentBuffer.Length < matcher.MaxMatchLength
				 || !tai.Advance(tai.CurrentBuffer.Length - matcher.MaxMatchLength))
				{
					break;
				}
				yield return new Checkpoint()
				{
					Position = tai.CharIndexToPosition(0),
					IsMatch = false
				};
				progressAndCancellation.HandleTextIterationProgress(tai);
				progressAndCancellation.CheckTextIterationCancellation();
			}
		}

		static IEnumerable<FileRange.Range> IterateMatchRanges(
			IEnumerable<Checkpoint> checkpoints, long threshhold, ProgressAndCancellation progressAndCancellation)
		{
			FileRange.Range? lastMatch = null;
			foreach (var checkpoint in checkpoints)
			{
				if (lastMatch == null)
				{
					if (checkpoint.IsMatch)
						lastMatch = new FileRange.Range(checkpoint.Position, checkpoint.EndPosition);
					else
						progressAndCancellation.continuationToken.NextPosition = checkpoint.EndPosition;
				}
				else
				{
					FileRange.Range lastMatchVal = lastMatch.Value;
					if (checkpoint.Position - lastMatchVal.End < threshhold)
					{
						if (checkpoint.IsMatch)
							lastMatch = new FileRange.Range(lastMatchVal.Begin, checkpoint.EndPosition);
					}
					else
					{
						yield return lastMatchVal;
						progressAndCancellation.continuationToken.NextPosition = checkpoint.EndPosition;
						if (checkpoint.IsMatch)
							lastMatch = new FileRange.Range(checkpoint.Position, checkpoint.EndPosition);
						else
							lastMatch = null;
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
						this.ExternalPostprocessingResult = externalPostprocessor != null ? externalPostprocessor(msg) : null;
					}
				}
			}
			public void CheckAgainstSearchCriteria(IMessage msg, SearchAllOccurencesThreadLocalData data)
			{
				this.PassedSearchCriteria = data.BulkProcessing.ProcessMessage(msg) == FilterAction.Include;
				this.CheckedAgainstSearchCriteria = true;
			}
		};

		class SearchAllOccurencesThreadLocalData
		{
			public IFiltersListBulkProcessing BulkProcessing;
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
				Search.Options fixedOptions = new Search.Options();
				plainTextSearchOptimizationPossible = true;

				if (!plainTextSearchOptimizationAllowed)
				{
					plainTextSearchOptimizationPossible = false;
				}
				else if (p.SearchParams.Filters.Items.Count() == 1 && p.SearchParams.Filters.GetDefaultAction() == FilterAction.Exclude)
				{
					var theOnlyPositiveFilter = p.SearchParams.Filters.Items.First();
					if (theOnlyPositiveFilter.Options.Template.Length == 0)
					{
						plainTextSearchOptimizationPossible = false;
					}
					else if (theOnlyPositiveFilter.Options.Regexp) // todo: detect and handle fixed-length regexps
					{
						plainTextSearchOptimizationPossible = false;
					}
					else
					{
						maxMatchLength = theOnlyPositiveFilter.Options.Template.Length;
						fixedOptions = theOnlyPositiveFilter.Options;
					}
				}
				if (plainTextSearchOptimizationPossible)
				{
					fixedOptions.ReverseSearch = false;
					opts = fixedOptions.BeginSearch();
				}
			}

			public bool PlainTextSearchOptimizationPossible { get { return plainTextSearchOptimizationPossible; } }

			public int MaxMatchLength { get { return maxMatchLength; } }

			public Search.MatchedTextRange? Match(StringSlice s, int startIndex)
			{
				return Search.SearchInText(s, opts, startIndex);
			}

			bool plainTextSearchOptimizationPossible;
			int maxMatchLength;
			Search.SearchState opts;
		};

		class ProgressAndCancellation
		{
			public Action<long> progressHandler;
			public CancellationToken cancellationToken;
			public int blocksReadSinseLastProgressUpdate;
			public int lastTimeHandlerWasCalled = Environment.TickCount;
			public int messagesReadSinseLastProgressUpdate;
			public ContinuationToken continuationToken;

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

			public void CheckTextIterationCancellation()
			{
				if (cancellationToken.IsCancellationRequested)
				{
					throw new SearchCancelledException() { ContinuationToken = continuationToken };
				}
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

		class ContinuationToken
		{
			public long NextPosition;
		};
	};
}
