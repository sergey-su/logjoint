using LogJoint.LogMedia;
using LogJoint.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LogJoint
{
    class FilteringMessagesReader : IMessagesReader
    {
        private static readonly UnicodeEncoding unicodeEncodingNoBOM = new(bigEndian: false, byteOrderMark: false);

        private readonly IMessagesReader unfilteredReader;
        private readonly IFiltersList filters;

        private bool filteringEnabled;
        private string filteredLogFile;
        private SimpleFileMedia filteredLogMedia;
        private IMessagesReader filteredLogReader;
        private bool filteredMessagesIsDirty = false;
        private readonly Func<ValueTask> ensureFilteredLogIsCreated;

        public FilteringMessagesReader(IMessagesReader unfilteredReader, MediaBasedReaderParams unfilteredReaderParams, IFiltersList filters,
            ITempFilesManager tempFilesManager, IFileSystem fileSystem, IRegexFactory regexFactory,
            ITraceSourceFactory traceSourceFactory, Settings.IGlobalSettingsAccessor globalSettings)
        {
            this.unfilteredReader = unfilteredReader;
            this.filters = filters;

            if (filters != null)
            {
                filters.OnFilteringEnabledChanged += (sender, evt) => filteredMessagesIsDirty = true;
                filters.OnFiltersListChanged += (sender, evt) => filteredMessagesIsDirty = true;
                filters.OnPropertiesChanged += (sender, evt) => filteredMessagesIsDirty = true;
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
                filteredLogReader = new XmlFormat.MessagesReader(
                    filteredReaderParams,
                    XmlFormat.XmlFormatInfo.MakeNativeFormatInfo(
                        unicodeEncodingNoBOM.WebName, null, new FormatViewOptions(rawViewAllowed: true), regexFactory),
                    regexFactory,
                    traceSourceFactory,
                    globalSettings,
                    useEmbeddedAttributes: true
                );
            };
        }

        long IMessagesReader.BeginPosition => unfilteredReader.BeginPosition;

        long IMessagesReader.EndPosition => unfilteredReader.EndPosition;

        async Task<UpdateBoundsStatus> IMessagesReader.UpdateAvailableBounds(bool incrementalMode)
        {
            // The filtering is done on update in this method.
            UpdateBoundsStatus status = await unfilteredReader.UpdateAvailableBounds(incrementalMode);
            bool filteringEnabled = filters != null && filters.FilteringEnabled && filters.Items.Count > 0;

            if (filteringEnabled && (status != UpdateBoundsStatus.NothingUpdated || filteredMessagesIsDirty))
            {
                await ensureFilteredLogIsCreated();
                await UpdateFilteredLog();
                await filteredLogReader.UpdateAvailableBounds(/*incrementalMode=*/false);
                filteredMessagesIsDirty = false;
            }

            this.filteringEnabled = filteringEnabled;

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
                filteredMessagesIsDirty = true;
            }
        }

        IAsyncEnumerable<PostprocessedMessage> IMessagesReader.Read(ReadMessagesParams p)
        {
            return filteringEnabled ? ReadFromFilteredLog(p) : unfilteredReader.Read(p);
        }

        IAsyncEnumerable<SearchResultMessage> IMessagesReader.Search(SearchMessagesParams p)
        {
            return filteringEnabled ? SearchInFilteredLog(p) : unfilteredReader.Search(p);
        }

        void IDisposable.Dispose()
        {
            unfilteredReader.Dispose();
            filteredLogReader?.Dispose();
            filteredLogMedia?.Dispose();
        }

        async ValueTask UpdateFilteredLog()
        {
            using var outputStream = new FileStream(filteredLogFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
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
                }
            );
            await foreach (SearchResultMessage msg in unfilteredReader.Search(new SearchMessagesParams()
            {
                Range = new FileRange.Range(unfilteredReader.BeginPosition, unfilteredReader.EndPosition),
                Flags = ReadMessagesFlag.HintMassiveSequentialReading,
                SearchParams = new SearchAllOccurencesParams(filters, searchInRawText: false, fromPosition: null)
            }))
            {
                outputWriter.WriteStartElement("m");
                outputWriter.WriteAttributeString("d", Listener.FormatDate(msg.Message.Time.ToUnspecifiedTime()));
                outputWriter.WriteAttributeString("t", msg.Message.Thread?.ID ?? "");
                outputWriter.WriteAttributeString("p", msg.Message.Position.ToString());
                outputWriter.WriteAttributeString("ep", msg.Message.EndPosition.ToString());
                if (msg.Message.RawText.IsInitialized)
                {
                    outputWriter.WriteAttributeString("r", msg.Message.RawText.ToString());
                }
                outputWriter.WriteString(msg.Message.Text);
                outputWriter.WriteEndElement();
            }
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
}
