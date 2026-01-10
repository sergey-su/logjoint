using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.Search;

namespace LogJoint
{
    class StreamSearching : IAsyncDisposable
    {
        readonly IMessagesReader owner;
        readonly SearchMessagesParams parserParams;
        readonly StreamReorderingParams? dejitteringParams;
        readonly TextStreamPositioningParams textStreamPositioningParams;
        readonly Stream rawStream;
        readonly Encoding streamEncoding;
        readonly bool plainTextSearchOptimizationAllowed;
        readonly FileRange.Range requestedRange;
        readonly ProgressAndCancellation progressAndCancellation;
        readonly StreamTextAccess aligmentTextAccess;
        readonly MessagesSplitter aligmentSplitter;
        readonly TextMessageCapture aligmentCapture;
        readonly IAsyncEnumerable<SearchResultMessage> impl;
        readonly LJTraceSource trace;
        readonly RegularExpressions.IRegexFactory regexFactory;
        IAsyncEnumerator<SearchResultMessage> enumerator;
        readonly IFilter dummyFilter;

        public static async IAsyncEnumerable<SearchResultMessage> Search(IMessagesReader owner,
            SearchMessagesParams p,
            TextStreamPositioningParams textStreamPositioningParams,
            StreamReorderingParams? dejitteringParams,
            Stream rawStream,
            Encoding streamEncoding,
            bool allowPlainTextSearchOptimization,
            LoadedRegex headerRe,
            ITraceSourceFactory traceSourceFactory,
            RegularExpressions.IRegexFactory regexFactory)
        {
            await using var parser = new StreamSearching(owner, p, textStreamPositioningParams, dejitteringParams,
                rawStream, streamEncoding, allowPlainTextSearchOptimization, headerRe, traceSourceFactory, regexFactory);
            for (; ; )
            {
                SearchResultMessage message = await parser.GetNext();
                if (message.Message == null)
                    break;
                yield return message;
            }
        }

        private StreamSearching(
            IMessagesReader owner,
            SearchMessagesParams p,
            TextStreamPositioningParams textStreamPositioningParams,
            StreamReorderingParams? dejitteringParams,
            Stream rawStream,
            Encoding streamEncoding,
            bool allowPlainTextSearchOptimization,
            LoadedRegex headerRe,
            ITraceSourceFactory traceSourceFactory,
            RegularExpressions.IRegexFactory regexFactory
        )
        {
            this.owner = owner;
            this.parserParams = p;
            this.plainTextSearchOptimizationAllowed = allowPlainTextSearchOptimization && ((p.Flags & ReadMessagesFlag.DisablePlainTextSearchOptimization) == 0);
            this.requestedRange = p.Range;
            this.textStreamPositioningParams = textStreamPositioningParams;
            this.dejitteringParams = dejitteringParams;
            this.rawStream = rawStream;
            this.streamEncoding = streamEncoding;
            this.regexFactory = regexFactory;
            this.trace = traceSourceFactory.CreateTraceSource("LogSource", "srchp." + GetHashCode().ToString("x"));
            this.dummyFilter = new Filter(FilterAction.Include, "", true, new Search.Options(), null, null, regexFactory);
            if (p.ContinuationToken as ContinuationToken != null)
                this.requestedRange = new FileRange.Range((p.ContinuationToken as ContinuationToken).NextPosition, requestedRange.End);
            this.aligmentTextAccess = new StreamTextAccess(rawStream, streamEncoding, textStreamPositioningParams);
            this.aligmentSplitter = new MessagesSplitter(aligmentTextAccess, headerRe.Regex, headerRe.GetHeaderReSplitterFlags());
            this.aligmentCapture = new TextMessageCapture();
            this.progressAndCancellation = new ProgressAndCancellation()
            {
                progressHandler = p.ProgressHandler,
                cancellationToken = p.Cancellation,
                continuationToken = new ContinuationToken() { NextPosition = requestedRange.Begin }
            };
            this.impl = Enum();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (enumerator != null)
                await enumerator.DisposeAsync();
        }

        private async ValueTask<SearchResultMessage> GetNext()
        {
            if (enumerator == null)
                enumerator = impl.GetAsyncEnumerator();
            if (!await enumerator.MoveNextAsync())
                return new SearchResultMessage();
            return enumerator.Current;
        }

        class MessagesPostprocessor : IMessagesPostprocessor
        {
            readonly IFiltersListBulkProcessing bulkProcessing;
            readonly Stopwatch filteringTime;
            readonly int tid;
            readonly LJTraceSource trace;
            readonly IFilter dummyFilter;
            int totalMessages;
            int matchedMessages;

            public MessagesPostprocessor(SearchAllOccurencesParams searchParams, LJTraceSource trace, IFilter dummyFilter)
            {
                this.dummyFilter = dummyFilter;
                this.bulkProcessing = searchParams.Filters.StartBulkProcessing(
                    MessageTextGetters.Get(searchParams.SearchInRawText),
                    reverseMatchDirection: false);
                this.filteringTime = new Stopwatch();
                this.tid = Thread.CurrentThread.ManagedThreadId;
                this.trace = trace;
            }

            public void Dispose()
            {
                trace.Info("Stats: filtering stats by thread {0}: time taken={1}, counters={2}/{3}",
                    tid, filteringTime.Elapsed, matchedMessages, totalMessages);
            }

            public object? Postprocess(IMessage msg)
            {
                ++totalMessages;
                filteringTime.Start();
                var rslt = bulkProcessing.ProcessMessage(msg, null);
                IFilter? ret;
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

            public static MessageFilteringResult GetFilteringResultFromPostprocessorResult(object obj, IFilter dummyFilter)
            {
                var f = (IFilter)obj;
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

        async IAsyncEnumerable<SearchResultMessage> Enum()
        {
            IMessagesPostprocessor postprocessor() => new MessagesPostprocessor(parserParams.SearchParams, trace, dummyFilter);
            long searchableRangesLength = 0;
            int searchableRangesCount = 0;
            long totalMessagesCount = 0;
            long totalHitsCount = 0;
            await foreach (var currentSearchableRange in EnumSearchableRanges())
            {
                searchableRangesLength += currentSearchableRange.Length;
                ++searchableRangesCount;
                long messagesCount = 0;
                long hitsCount = 0;
                await foreach (PostprocessedMessage tmp in CreateParserForSearchableRange(currentSearchableRange, postprocessor))
                {
                    ++messagesCount;

                    var msg = tmp.Message;
                    var filteringResult = MessagesPostprocessor.GetFilteringResultFromPostprocessorResult(
                        tmp.PostprocessingResult, dummyFilter);

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
            trace.Info("Stats: searchable ranges count: {0}", searchableRangesCount);
            trace.Info("Stats: ave searchable range len: {0}",
                        searchableRangesCount != 0 ? searchableRangesLength / searchableRangesCount : 0);
            PrintPctStats("searchable ranges coverage pct", searchableRangesLength, requestedRange.Length);
            PrintPctStats("hits pct overall", totalHitsCount, totalMessagesCount);
        }

        void PrintPctStats(string name, long num, long denum)
        {
            trace.Info("Stats: {0}: {1:F4}%", name, denum != 0 ? num * 100d / denum : 0d);
        }

        async IAsyncEnumerable<FileRange.Range> EnumSearchableRanges()
        {
            var matcher = new PlainTextMatcher(parserParams, textStreamPositioningParams, plainTextSearchOptimizationAllowed, regexFactory);
            if (!matcher.PlainTextSearchOptimizationPossible)
            {
                yield return requestedRange;
                yield break;
            }
            long? skipRangesDownThisPosition = null;
            await foreach (var currentRange in EnumSearchableRangesCore(matcher))
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

        IAsyncEnumerable<PostprocessedMessage> CreateParserForSearchableRange(
            FileRange.Range searchableRange,
            Func<IMessagesPostprocessor> messagesPostprocessor)
        {
            bool disableMultithreading = false;
            return owner.Read(new ReadMessagesParams(
                searchableRange.Begin, searchableRange,
                ReadMessagesFlag.HintMassiveSequentialReading
                | (disableMultithreading ? ReadMessagesFlag.DisableMultithreading : ReadMessagesFlag.None),
                ReadMessagesDirection.Forward,
                messagesPostprocessor));
        }

        async IAsyncEnumerable<FileRange.Range> EnumSearchableRangesCore(PlainTextMatcher matcher)
        {
            ITextAccess ta = new StreamTextAccess(rawStream, streamEncoding, textStreamPositioningParams);
            using var tai = await ta.OpenIterator(requestedRange.Begin, TextAccessDirection.Forward);
            var lastRange = new FileRange.Range();
            await foreach (var r in IterateMatchRanges(
                EnumCheckpoints(tai, matcher, progressAndCancellation, trace),
                // todo: tune next parameter to find the value giving max performance.
                // On one sample log bigger block was better than many small ones. 
                // Hence quite big threshold.
                textStreamPositioningParams.AlignmentBlockSize * 8,
                progressAndCancellation
            ))
            {
                var postprocessedRange = await PostprocessHintRange(r, lastRange);
                lastRange = postprocessedRange;
                yield return postprocessedRange;
            }
        }

        async Task<FileRange.Range> PostprocessHintRange(FileRange.Range r, FileRange.Range lastRange)
        {
            long fixedBegin = r.Begin;
            long fixedEnd;

            int? inflateRangeBy = null;
            if (dejitteringParams != null && (parserParams.Flags & ReadMessagesFlag.DisableDejitter) == 0)
                inflateRangeBy = dejitteringParams.Value.JitterBufferSize;

            await aligmentSplitter.BeginSplittingSession(requestedRange, r.End, ReadMessagesDirection.Forward);
            if (await aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
            {
                fixedEnd = aligmentCapture.EndPosition;
                if (inflateRangeBy != null)
                {
                    for (int i = 0; i < inflateRangeBy.Value; ++i)
                    {
                        if (!await aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
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

            await aligmentSplitter.BeginSplittingSession(requestedRange, fixedBegin, ReadMessagesDirection.Backward);
            if (await aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
            {
                fixedBegin = aligmentCapture.BeginPosition;
                if (inflateRangeBy != null)
                {
                    for (int i = 0; i < inflateRangeBy.Value; ++i)
                    {
                        if (!await aligmentSplitter.GetCurrentMessageAndMoveToNextOne(aligmentCapture))
                            break;
                        fixedBegin = aligmentCapture.BeginPosition;
                    }
                }
            }
            else
            {
                fixedBegin = owner.BeginPosition;
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

        static async IAsyncEnumerable<Checkpoint> EnumCheckpoints(
            ITextAccessIterator tai, PlainTextMatcher matcher,
            ProgressAndCancellation progressAndCancellation,
            LJTraceSource trace)
        {
            var advanceTime = new Stopwatch();
            long advancesCount = 0;
            var matchingTime = new Stopwatch();
            long matchCount = 0;
            for (; ; )
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
                bool stop = !await tai.Advance(Math.Max(0, tai.CurrentBuffer.Length - matcher.MaxMatchLength));
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
                TimeSpan.FromTicks(advanceTime.ElapsedTicks / Math.Max(1, advancesCount)));
        }

        static async IAsyncEnumerable<FileRange.Range> IterateMatchRanges(
            IAsyncEnumerable<Checkpoint> checkpoints, long threshhold, ProgressAndCancellation progressAndCancellation)
        {
            FileRange.Range? lastMatch = null;
            await foreach (Checkpoint checkpoint in checkpoints)
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
                SearchMessagesParams p,
                TextStreamPositioningParams textStreamPositioningParams,
                bool plainTextSearchOptimizationAllowed,
                RegularExpressions.IRegexFactory regexFactory)
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
                            maxMatchLength = Math.Max(maxMatchLength, filterMaxMatchLength);
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
                    opts = fixedOptions.Select(i => i.BeginSearch(regexFactory)).ToArray();
                }
            }

            public bool PlainTextSearchOptimizationPossible { get { return plainTextSearchOptimizationPossible; } }

            public int MaxMatchLength { get { return maxMatchLength; } }

            public Search.MatchedTextRange? Match(StringSlice s, int startIndex)
            {
                if (opts == null)
                    return null;
                foreach (var i in opts)
                {
                    var tmp = i.SearchInText(s, startIndex);
                    if (tmp != null)
                        return tmp;
                }
                return null;
            }

            readonly bool plainTextSearchOptimizationPossible;
            readonly int maxMatchLength;
            readonly Search.SearchState[]? opts;
        };

        class ProgressAndCancellation
        {
            required public Action<long>? progressHandler;
            required public CancellationToken cancellationToken;
            public int blocksReadSinseLastProgressUpdate;
            public int lastTimeHandlerWasCalled = Environment.TickCount;
            public int messagesReadSinseLastProgressUpdate;
            required public ContinuationToken continuationToken;

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
