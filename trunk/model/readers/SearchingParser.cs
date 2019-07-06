﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LogJoint.Search;

namespace LogJoint
{
	class SearchingParser : ISearchingParser
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
		readonly StreamTextAccess aligmentTextAccess;
		readonly MessagesSplitter aligmentSplitter;
		readonly TextMessageCapture aligmentCapture;
		readonly IEnumerable<SearchResultMessage> impl;
		readonly LJTraceSource trace;
		IEnumerator<SearchResultMessage> enumerator;

		public SearchingParser(
			IPositionedMessagesReader owner,
			CreateSearchingParserParams p,
			TextStreamPositioningParams textStreamPositioningParams,
			DejitteringParams? dejitteringParams,
			Stream rawStream,
			Encoding streamEncoding,
			bool allowPlainTextSearchOptimization,
			LoadedRegex headerRe,
			ILogSourceThreads threads,
			ITraceSourceFactory traceSourceFactory
		)
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
			this.trace = traceSourceFactory.CreateTraceSource("LogSource", "srchp." + GetHashCode().ToString("x"));
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
			this.impl = Enum();
		}

		SearchResultMessage ISearchingParser.GetNext()
		{
			if (enumerator == null)
				enumerator = impl.GetEnumerator();
			if (!enumerator.MoveNext())
				return new SearchResultMessage();
			return enumerator.Current;
		}

		public void Dispose()
		{
			enumerator?.Dispose();
		}

		class MessagesPostprocessor : IMessagesPostprocessor
		{
			readonly IFiltersListBulkProcessing bulkProcessing;
			readonly Stopwatch filteringTime;
			readonly int tid;
			readonly LJTraceSource trace;
			int totalMessages;
			int matchedMessages;

			static readonly IFilter dummyFilter = new Filter(
				FilterAction.Include, "", true, new Search.Options(), null);

			public MessagesPostprocessor(SearchAllOccurencesParams searchParams, LJTraceSource trace)
			{
				this.bulkProcessing = searchParams.Filters.StartBulkProcessing(
					MessageTextGetters.Get(searchParams.SearchInRawText),
					reverseMatchDirection: false);
				this.filteringTime = new Stopwatch();
				this.tid = Thread.CurrentThread.ManagedThreadId;
				this.trace = trace;
			}

			public void Dispose ()
			{
				trace.Info("Stats: filtering stats by thread {0}: time taken={1}, counters={2}/{3}", 
					tid, filteringTime.Elapsed, matchedMessages, totalMessages);
			}

			public object Postprocess (IMessage msg)
			{
				++totalMessages;
				filteringTime.Start();
				var rslt = bulkProcessing.ProcessMessage(msg, null);
				IFilter ret;
				if (rslt.Action == FilterAction.Exclude)
				{
					ret = null;
				}
				else
				{
					ret = rslt.Filter ?? dummyFilter;
					++matchedMessages;
				}
				filteringTime.Stop();
				return ret;
			}

			public static MessageFilteringResult GetFilteringResultFromPostprocessorResult(object obj)
			{
				var f = (IFilter) obj;
				if (f == null)
					return new MessageFilteringResult { Action = FilterAction.Exclude };
				if (f == dummyFilter)
					return new MessageFilteringResult { Action = FilterAction.Include };
				return new MessageFilteringResult
				{
					Action = f.Action,
					Filter = f
				};
			}
		};

		IEnumerable<SearchResultMessage> Enum()
		{
			using (var threadsBulkProcessing = threads.UnderlyingThreadsContainer.StartBulkProcessing())
			{
				Func<IMessagesPostprocessor> postprocessor = 
					() => new MessagesPostprocessor(parserParams.SearchParams, trace);
				long searchableRangesLength = 0;
				int searchableRangesCount = 0;
				long totalMessagesCount = 0;
				long totalHitsCount = 0;
				foreach (var currentSearchableRange in EnumSearchableRanges())
				{
					searchableRangesLength += currentSearchableRange.Length;
					++searchableRangesCount;
					using (var parser = CreateParserForSearchableRange(currentSearchableRange, postprocessor))
					{
						long messagesCount = 0;
						long hitsCount = 0;
						for (;;)
						{
							var tmp = parser.ReadNextAndPostprocess();
							if (tmp.Message == null)
								break;

							++messagesCount;

							var msg = tmp.Message;
							var filteringResult = MessagesPostprocessor.GetFilteringResultFromPostprocessorResult(
								tmp.PostprocessingResult);

							if (filteringResult.Action != FilterAction.Exclude)
							{
								++hitsCount;
								yield return new SearchResultMessage(msg, filteringResult);
							}

							progressAndCancellation.HandleMessageReadingProgress(msg.Position);
							progressAndCancellation.continuationToken.NextPosition = msg.EndPosition;

							progressAndCancellation.CheckTextIterationCancellation();
						}
						PrintPctStats(string.Format("hits pct in range {0}", currentSearchableRange), 
							hitsCount, messagesCount); 
						totalMessagesCount += messagesCount;
						totalHitsCount += hitsCount;
					}
				}
				trace.Info("Stats: searchable ranges count: {0}", searchableRangesCount);
				trace.Info("Stats: ave searchable range len: {0}", 
				           searchableRangesCount != 0 ? searchableRangesLength / searchableRangesCount : 0);
				PrintPctStats("searchable ranges coverage pct", searchableRangesLength, requestedRange.Length); 
				PrintPctStats("hits pct overall", totalHitsCount, totalMessagesCount); 
			}

			yield return new SearchResultMessage(null, new MessageFilteringResult());
		}

		void PrintPctStats(string name, long num, long denum)
		{
			trace.Info("Stats: {0}: {1:F4}%", name, denum != 0 ? num * 100d / denum : 0d);			
		}

		IEnumerable<FileRange.Range> EnumSearchableRanges()
		{
			var matcher = new PlainTextMatcher(parserParams, textStreamPositioningParams, plainTextSearchOptimizationAllowed);
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
			}
		}

		IPositionedMessagesParser CreateParserForSearchableRange(
			FileRange.Range searchableRange,
			Func<IMessagesPostprocessor> messagesPostprocessor)
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
			ITextAccess ta = new StreamTextAccess(rawStream, streamEncoding, textStreamPositioningParams);
			using (var tai = ta.OpenIterator(requestedRange.Begin, TextAccessDirection.Forward))
			{
				var lastRange = new FileRange.Range();
				foreach (var r in
					IterateMatchRanges(
						EnumCheckpoints(tai, matcher, progressAndCancellation, trace),
						// todo: tune next parameter to find the value giving max performance.
						// On one sample log bigger block was better than many small ones. 
						// Hence quite big threshold.
						textStreamPositioningParams.AlignmentBlockSize * 8,
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

		struct Checkpoint
		{
			public long Position, EndPosition;
			public bool IsMatch;
		};

		static IEnumerable<Checkpoint> EnumCheckpoints(
			ITextAccessIterator tai, PlainTextMatcher matcher, 
			ProgressAndCancellation progressAndCancellation,
			LJTraceSource trace)
		{
			var advanceTime = new Stopwatch();
			long advancesCount = 0;
			var matchingTime = new Stopwatch();
			long matchCount = 0;
			for (;;)
			{
				StringSlice buf = new StringSlice(tai.CurrentBuffer);
				for (int startIdx = 0; ;)
				{
					matchingTime.Start();
					var match = matcher.Match(buf, startIdx);
					matchingTime.Stop();
					++matchCount;
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
				advanceTime.Start();
				bool stop = !tai.Advance(Math.Max(0, tai.CurrentBuffer.Length - matcher.MaxMatchLength));
				advanceTime.Stop();
				++advancesCount;
				if (stop)
				{
					break;
				}
				yield return new Checkpoint()
				{
					EndPosition = tai.CharIndexToPosition(0),
					IsMatch = false
				};
				progressAndCancellation.CheckTextIterationCancellation();
			}
			trace.Info("Stats: text buffer matching time: {0} ({1} times)", 
				matchingTime.Elapsed, matchCount);
			trace.Info("Stats: text buffer advance time: {0}/{1}={2}", 
				advanceTime.Elapsed, advancesCount, 
				TimeSpan.FromTicks(advanceTime.ElapsedTicks/Math.Max(1, advancesCount)));
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
					{
						progressAndCancellation.continuationToken.NextPosition = checkpoint.EndPosition;
						progressAndCancellation.HandleTextIterationProgress(checkpoint.EndPosition);
					}
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
						progressAndCancellation.HandleTextIterationProgress(checkpoint.EndPosition);
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

		class PlainTextMatcher
		{
			public PlainTextMatcher(
				CreateSearchingParserParams p, 
				TextStreamPositioningParams textStreamPositioningParams,
				bool plainTextSearchOptimizationAllowed)
			{
				var fixedOptions = new List<Search.Options>();
				plainTextSearchOptimizationPossible = true;

				if (!plainTextSearchOptimizationAllowed)
				{
					plainTextSearchOptimizationPossible = false;
				}
				else if (p.SearchParams.Filters.GetDefaultAction() == FilterAction.Exclude)
				{
					// todo: handle case of multiple positive filters
					foreach (var filter in p.SearchParams.Filters.Items)
					{
						if (filter.Options.Template.Length == 0)
						{
							plainTextSearchOptimizationPossible = false;
						}
						else
						{
							var filterMaxMatchLength = filter.Options.Regexp 
								? textStreamPositioningParams.AlignmentBlockSize
								: filter.Options.Template.Length;
							maxMatchLength = Math.Max(maxMatchLength, filter.Options.Template.Length);
							var tmp = filter.Options;
							tmp.ReverseSearch = false;
							fixedOptions.Add(tmp);
						};
					};
				}
				else
				{
					plainTextSearchOptimizationPossible = false;
				}
				if (plainTextSearchOptimizationPossible)
				{
					opts = fixedOptions.Select(i => i.BeginSearch()).ToArray();
				}
			}

			public bool PlainTextSearchOptimizationPossible { get { return plainTextSearchOptimizationPossible; } }

			public int MaxMatchLength { get { return maxMatchLength; } }

			public Search.MatchedTextRange? Match(StringSlice s, int startIndex)
			{
				foreach (var i in opts)
				{
					var tmp = i.SearchInText(s, startIndex);
					if (tmp != null)
						return tmp;
				}
				return null;
			}

			bool plainTextSearchOptimizationPossible;
			int maxMatchLength;
			Search.SearchState[] opts;
		};

		class ProgressAndCancellation
		{
			public Action<long> progressHandler;
			public CancellationToken cancellationToken;
			public int blocksReadSinseLastProgressUpdate;
			public int lastTimeHandlerWasCalled = Environment.TickCount;
			public int messagesReadSinseLastProgressUpdate;
			public ContinuationToken continuationToken;

			public void HandleTextIterationProgress(long pos)
			{
				int checkProgressConditionEvery = 64;
				if (progressHandler != null)
				{
					if ((++blocksReadSinseLastProgressUpdate % checkProgressConditionEvery) == 0)
					{
						int now = Environment.TickCount;
						if (now - lastTimeHandlerWasCalled > 1000)
						{
							progressHandler(pos);
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
