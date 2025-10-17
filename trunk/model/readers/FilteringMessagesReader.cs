using ICSharpCode.SharpZipLib.Core;
using LogJoint.LogMedia;
using LogJoint.Progress;
using LogJoint.RegularExpressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LogJoint
{
    class FilteringMessagesReader : IMessagesReader
    {
        private static readonly UnicodeEncoding unicodeEncodingNoBOM = new(bigEndian: false, byteOrderMark: false);

        private readonly IMessagesReader unfilteredReader;
        private readonly ISynchronizationContext modelSynchronizationContext;
        private readonly IFiltersList modelFilters; // The filters shared with the model threading context.
        private readonly FilteringStats filteringStats;

        private IFiltersList effectiveFilters; // The filters currently used for filtering in this IMessagesReader's context.
        private string filteredLogFile;
        private SimpleFileMedia filteredLogMedia;
        private IMessagesReader filteredLogReader;
        private bool modelFiltersChanged = false; // The flag lives in the model threading context.
        private bool timeOffsetChanged = false;
        private readonly Func<ValueTask> ensureFilteredLogIsCreated;

        public FilteringMessagesReader(IMessagesReader unfilteredReader, MediaBasedReaderParams unfilteredReaderParams, IFiltersList filters,
            ITempFilesManager tempFilesManager, IFileSystem fileSystem, IRegexFactory regexFactory,
            ITraceSourceFactory traceSourceFactory, Settings.IGlobalSettingsAccessor globalSettings,
            ISynchronizationContext modelSynchronizationContext, FilteringStats filteringStats)
        {
            this.unfilteredReader = unfilteredReader;
            this.modelSynchronizationContext = modelSynchronizationContext;
            this.modelFilters = filters;
            this.filteringStats = filteringStats;

            if (filters != null)
            {
                filters.OnFilteringEnabledChanged += (sender, evt) => modelFiltersChanged = true;
                filters.OnFiltersListChanged += (sender, evt) => modelFiltersChanged = true;
                filters.OnPropertiesChanged += (sender, evt) => modelFiltersChanged = true;
            }

            ensureFilteredLogIsCreated = async () =>
            {
                if (filteredLogReader != null)
                {
                    return;
                }
                filteredLogFile = tempFilesManager.CreateEmptyFile();
                filteredLogMedia = await SimpleFileMedia.Create(
                    fileSystem, SimpleFileMedia.CreateConnectionParamsFromFileName(filteredLogFile));
                MediaBasedReaderParams filteredReaderParams = unfilteredReaderParams;
                filteredReaderParams.Media = filteredLogMedia;
                filteredReaderParams.ParentLoggingPrefix = (unfilteredReaderParams.ParentLoggingPrefix ?? "") + ".filtered";
                await modelSynchronizationContext.Invoke(() =>
                {
                    filteredLogReader = new XmlFormat.MessagesReader(
                        filteredReaderParams,
                        XmlFormat.XmlFormatInfo.MakeNativeFormatInfo(
                            unicodeEncodingNoBOM.WebName, null, new TextStreamPositioningParams(1024 * 1024),
                            new FormatViewOptions(rawViewAllowed: true), regexFactory),
                        regexFactory,
                        traceSourceFactory,
                        globalSettings,
                        useEmbeddedAttributes: true
                    );
                });
            };
        }

        long IMessagesReader.BeginPosition => unfilteredReader.BeginPosition;

        long IMessagesReader.EndPosition => unfilteredReader.EndPosition;

        async Task<UpdateBoundsStatus> IMessagesReader.UpdateAvailableBounds(bool incrementalMode)
        {
            // The filtering is done on update in this method.
            UpdateBoundsStatus status = await unfilteredReader.UpdateAvailableBounds(incrementalMode);
            bool oldFilteringEnabled = this.effectiveFilters != null;
            bool filteringEnabled = oldFilteringEnabled;
            bool filtersChanged = false;
            await modelSynchronizationContext.Invoke(() =>
            {
                filteringEnabled = modelFilters != null && modelFilters.FilteringEnabled && modelFilters.Items.Count > 0;
                filtersChanged = this.modelFiltersChanged;
                this.modelFiltersChanged = false;

                // Ensure the invariant that effectiveFilters != null IFF filteringEnabled.
                if (!filteringEnabled)
                {
                    this.effectiveFilters = null;
                }
                else if (this.effectiveFilters == null || filtersChanged)
                {
                    this.effectiveFilters = modelFilters.Clone();
                }
            });
            bool timeOffsetChanged = this.timeOffsetChanged;
            this.timeOffsetChanged = false;

            if (filteringEnabled && (status != UpdateBoundsStatus.NothingUpdated || filtersChanged || timeOffsetChanged))
            {
                using IProgressEventsSink progressSink = filteringStats?.ScopedFilteringProgress();
                await ensureFilteredLogIsCreated();
                await UpdateFilteredLog(progressSink);
                await filteredLogReader.UpdateAvailableBounds(/*incrementalMode=*/false);
                return UpdateBoundsStatus.MessagesFiltered;
            }
            if (!filteringEnabled && oldFilteringEnabled)
            {
                return UpdateBoundsStatus.MessagesFiltered;
            }

            return status;
        }

        long IMessagesReader.MaximumMessageSize => unfilteredReader.MaximumMessageSize;

        long IMessagesReader.PositionRangeToBytes(FileRange.Range range) => unfilteredReader.PositionRangeToBytes(range);

        long IMessagesReader.SizeInBytes => unfilteredReader.SizeInBytes;

        ValueTask<int> IMessagesReader.GetContentsEtag() => unfilteredReader.GetContentsEtag();

        ITimeOffsets IMessagesReader.TimeOffsets
        {
            get
            {
                return unfilteredReader.TimeOffsets;
            }
            set
            {
                unfilteredReader.TimeOffsets = value;
                timeOffsetChanged = true;
            }
        }

        IAsyncEnumerable<PostprocessedMessage> IMessagesReader.Read(ReadMessagesParams p)
        {
            return effectiveFilters != null ? ReadFromFilteredLog(p) : unfilteredReader.Read(p);
        }

        IAsyncEnumerable<SearchResultMessage> IMessagesReader.Search(SearchMessagesParams p)
        {
            return effectiveFilters != null ? SearchInFilteredLog(p) : unfilteredReader.Search(p);
        }

        Encoding IMessagesReader.Encoding => unfilteredReader.Encoding;

        void IDisposable.Dispose()
        {
            unfilteredReader.Dispose();
            filteredLogReader?.Dispose();
            filteredLogMedia?.Dispose();
        }

        async Task UpdateFilteredLog(IProgressEventsSink progressSink)
        {
            await using var outputStream = new FileStream(
                filteredLogFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            outputStream.SetLength(0);
            using var outputWriter = XmlWriter.Create(
                outputStream,
                new XmlWriterSettings()
                {
                    CloseOutput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    OmitXmlDeclaration = false,
                    Indent = true,
                    Encoding = unicodeEncodingNoBOM,
                    Async = true,
                }
            );
            var range = new FileRange.Range(unfilteredReader.BeginPosition, unfilteredReader.EndPosition);
            await foreach (SearchResultMessage msg in unfilteredReader.Search(new SearchMessagesParams()
            {
                Range = range,
                ProgressHandler = pos => progressSink.SetValue((double)(pos - range.Begin) / (double)(range.Length > 0 ? range.Length : 1)),
                Flags = ReadMessagesFlag.HintMassiveSequentialReading,
                SearchParams = new SearchAllOccurencesParams(effectiveFilters, searchInRawText: false, fromPosition: null)
            }))
            {
                await outputWriter.WriteStartElementAsync(null, "m", null);
                await outputWriter.WriteAttributeStringAsync(null, "d", null, Listener.FormatDate(msg.Message.Time.ToUnspecifiedTime()));
                await outputWriter.WriteAttributeStringAsync(null, "t", null, msg.Message.Thread?.ID ?? "");
                await outputWriter.WriteAttributeStringAsync(null, "p", null, msg.Message.Position.ToString());
                await outputWriter.WriteAttributeStringAsync(null, "ep", null, msg.Message.EndPosition.ToString());
                if (msg.Message.RawText.IsInitialized)
                {
                    await outputWriter.WriteAttributeStringAsync(null, "r", null, msg.Message.RawText.ToString());
                }
                await outputWriter.WriteStringAsync(msg.Message.Text);
                await outputWriter.WriteEndElementAsync();
            }
            await outputWriter.FlushAsync();
            await outputStream.FlushAsync();
        }

        async IAsyncEnumerable<PostprocessedMessage> ReadFromFilteredLog(ReadMessagesParams p)
        {
            bool reverseDirection = p.Direction == ReadMessagesDirection.Backward;
            p.StartPosition = await MapToFilteredLogPosition(p.StartPosition, reverseDirection);
            if (p.Range.HasValue)
            {
                p.Range = new FileRange.Range(
                    await MapToFilteredLogPosition(p.Range.Value.Begin, reverseDirection),
                    await MapToFilteredLogPosition(p.Range.Value.End, reverseDirection));
            }
            await foreach (PostprocessedMessage msg in filteredLogReader.Read(p))
            {
                yield return new PostprocessedMessage(UseEmbeddedPositions(msg.Message), msg.PostprocessingResult);
            }
        }

        async IAsyncEnumerable<SearchResultMessage> SearchInFilteredLog(SearchMessagesParams p)
        {
            if (p.SearchParams.FromPosition.HasValue)
            {
                p.SearchParams = new SearchAllOccurencesParams(p.SearchParams.Filters,
                    p.SearchParams.SearchInRawText, await MapToFilteredLogPosition(p.SearchParams.FromPosition.Value, reverseDirection: false));
            }
            p.Range = new FileRange.Range(
                await MapToFilteredLogPosition(p.Range.Begin, reverseDirection: false),
                await MapToFilteredLogPosition(p.Range.End, reverseDirection: false));
            await foreach (SearchResultMessage msg in filteredLogReader.Search(p))
            {
                yield return new SearchResultMessage(UseEmbeddedPositions(msg.Message), msg.FilteringResult);
            }
        }

        // Finds the smallest filtered log position of messages whose embedded (unfiltered) log position
        // is greater than or equal to the given unfilteredLogPosition.
        async ValueTask<long> MapToFilteredLogPosition(long unfilteredLogPosition, bool reverseDirection)
        {
            long begin = filteredLogReader.BeginPosition;
            long end = filteredLogReader.EndPosition;
            long count = end - begin;
            long pos = begin;
            for (; 0 < count;)
            {
                long count2 = count / 2;
                long mid = pos + count2;

                IMessage msg = await PositionedMessagesUtils.ReadNearestMessage(filteredLogReader, mid,
                    ReadMessagesFlag.HintMessageContentIsNotNeeed);
                long embeddedPosition =
                    msg == null ? unfilteredReader.EndPosition : GetMandatoryEmbeddedPositions(msg).Position;
                bool moveRight;
                if (reverseDirection)
                {
                    moveRight = embeddedPosition <= unfilteredLogPosition;
                }
                else
                {
                    moveRight = embeddedPosition < unfilteredLogPosition;
                }
                if (moveRight)
                {
                    pos = ++mid;
                    count -= count2 + 1;
                }
                else
                {
                    count = count2;
                }
            }
            if (reverseDirection)
            {
                long? tmp = await PositionedMessagesUtils.FindPrevMessagePosition(filteredLogReader, pos);
                if (tmp == null)
                    return begin - 1;
                pos = tmp.Value;
            }
            return pos;
        }

        static Message.EmbeddedPositions GetMandatoryEmbeddedPositions(IMessage message)
        {
            Message.EmbeddedPositions embeddedPositions = ((Message)message).GetEmbeddedPosition();
            if (embeddedPositions == null)
                throw new InvalidDataException("Failed to extract embedded position");
            return embeddedPositions;
        }

        static IMessage UseEmbeddedPositions(IMessage message)
        {
            Message.EmbeddedPositions embeddedPositions = GetMandatoryEmbeddedPositions(message);
            message.SetPosition(embeddedPositions.Position, embeddedPositions.EndPosition);
            return message;
        }
    }

    // Thread-safe.
    public class FilteringStats
    {
        readonly IProgressAggregator progressAggregator;

        public FilteringStats(IProgressAggregatorFactory progressAggregatorFactory)
        {
            this.progressAggregator = progressAggregatorFactory.CreateProgressAggregator();
        }

        public double? FilteringProgress => progressAggregator.ProgressValue;

        public IProgressEventsSink ScopedFilteringProgress()
        {
            return progressAggregator.CreateProgressSink();
        }
    };
}
