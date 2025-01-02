using System;
using System.Text;
using System.IO;
using LogJoint.StreamReadingStrategies;
using LogJoint.RegularExpressions;
using System.Threading.Tasks;
using LogJoint.Postprocessing;
using System.Collections.Generic;

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
            bool isQuickFormatDetectionMode,
            Settings.IGlobalSettingsAccessor settingsAccessor,
            ITraceSourceFactory traceSourceFactory,
            string parentLoggingPrefix
        )
        {
            this.beginFinder = beginFinder;
            this.endFinder = endFinder;
            this.media = media;
            this.textStreamPositioningParams = textStreamPositioningParams;
            this.singleThreadedStrategy = new Lazy<BaseStrategy>(CreateSingleThreadedStrategy);
            this.multiThreadedStrategy = new Lazy<BaseStrategy>(CreateMultiThreadedStrategy);
            this.extensions = new MessagesReaderExtensions(this, extensionsInitData);
            this.isQuickFormatDetectionMode = isQuickFormatDetectionMode;
            this.settingsAccessor = settingsAccessor;
            this.trace = traceSourceFactory.CreateTraceSource("LogSource",
                string.Format("{0}.r{1:x4}", parentLoggingPrefix, Hashing.GetShortHashCode(this.GetHashCode())));
            this.perfCounters = new Profiling.Counters(this.trace, $"perf.{parentLoggingPrefix}");
            this.ReadMessageCounter = this.perfCounters.AddCounter(
                name: "read message time",
                unit: "ms",
                reportCount: true,
                reportAverage: true,
                reportMax: true,
                reportMin: true
            );
        }

        #region IPositionedMessagesReader

        public long BeginPosition => beginPosition.Value;

        public long EndPosition => endPosition.Value;

        public long MaximumMessageSize => textStreamPositioningParams.AlignmentBlockSize;

        public long PositionRangeToBytes(FileRange.Range range)
        {
            // Here calculation is not precise: TextStreamPosition cannot be converted to bytes 
            // directly and efficiently. But this function is used only for statistics so it's ok to 
            // use approximate calculations here.
            var encoding = StreamEncoding;
            return TextStreamPositionToStreamPosition_Approx(range.End, encoding, textStreamPositioningParams) - TextStreamPositionToStreamPosition_Approx(range.Begin, encoding, textStreamPositioningParams);
        }

        public long SizeInBytes => mediaSize;

        public ITimeOffsets TimeOffsets
        {
            get { return timeOffsets; }
            set { timeOffsets = value; }
        }

        public async Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode)
        {
            var incrementalModeRef = new Ref<bool>(incrementalMode);
            var ret = await UpdateAvailableBoundsInternal(incrementalModeRef);
            Extensions.NotifyExtensionsAboutUpdatedAvailableBounds(new AvailableBoundsUpdateNotificationArgs()
            {
                Status = ret,
                IsIncrementalMode = incrementalModeRef.Value,
                IsQuickFormatDetectionMode = this.IsQuickFormatDetectionMode
            });
            return ret;
        }

        public IAsyncEnumerable<PostprocessedMessage> Read(ReadMessagesParams parserParams)
        {
            // That's not the best place for flushing counters, but it's the only one that works in blazor
            // that lacks periodic calls to UpdateAvailableBounds.
            if (perfCounters.Report(atMostOncePer: TimeSpan.FromMilliseconds(500)))
                perfCounters.ResetAll();

            parserParams.EnsureRangeIsSet(this);

            var strategiesCache = new StreamReading.StrategiesCache()
            {
                MultiThreadedStrategy = multiThreadedStrategy,
                SingleThreadedStrategy = singleThreadedStrategy
            };

            DejitteringParams? dejitteringParams = GetDejitteringParams();
            if (dejitteringParams != null && (parserParams.Flags & ReadMessagesFlag.DisableDejitter) == 0)
            {
                return DejitteringMessagesParser.Create(
                    underlyingParserParams => StreamReading.Read(
                        this,
                        EnsureParserRangeDoesNotExceedReadersBoundaries(underlyingParserParams),
                        textStreamPositioningParams,
                        settingsAccessor,
                        strategiesCache
                    ),
                    parserParams,
                    dejitteringParams.Value
                );
            }
            return StreamReading.Read(
                this,
                parserParams,
                textStreamPositioningParams,
                settingsAccessor,
                strategiesCache
            );
        }

        public virtual Task<ISearchingParser> CreateSearchingParser(SearchMessagesParams p)
        {
            return Task.FromResult<ISearchingParser>(null);
        }

        async ValueTask<int> IPositionedMessagesReader.GetContentsEtag()
        {
            VolatileStream.Position = 0;
            byte[] buf = new byte[1024];
            int read = await VolatileStream.ReadAsync(buf, 0, buf.Length);
            return Hashing.GetStableHashCode(buf, 0, read);
        }

        public bool IsQuickFormatDetectionMode => isQuickFormatDetectionMode;

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

        public ILogMedia LogMedia => media;

        public Stream VolatileStream => media.DataStream;

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

        protected DateTime MediaLastModified => media.LastModified;

        protected MessagesReaderExtensions Extensions => extensions;

        protected static FieldsProcessor.MakeMessageFlags ParserFlagsToMakeMessageFlags(ReadMessagesFlag flags)
        {
            FieldsProcessor.MakeMessageFlags ret = FieldsProcessor.MakeMessageFlags.Default;
            if ((flags & ReadMessagesFlag.HintMessageTimeIsNotNeeded) != 0)
                ret |= FieldsProcessor.MakeMessageFlags.HintIgnoreTime;
            if ((flags & ReadMessagesFlag.HintMessageContentIsNotNeeed) != 0)
                ret |= (FieldsProcessor.MakeMessageFlags.HintIgnoreBody | FieldsProcessor.MakeMessageFlags.HintIgnoreEntryType
                    | FieldsProcessor.MakeMessageFlags.HintIgnoreSeverity | FieldsProcessor.MakeMessageFlags.HintIgnoreThread);
            return ret;
        }

        protected LJTraceSource Trace => trace;

        protected Profiling.Counters PerfCounters => perfCounters;

        protected Profiling.Counters.CounterDescriptor ReadMessageCounter { get; private set; }

        #endregion

        #region Implementation

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
            TextStreamPosition? pos = finder.Find(stm, encoding, boundName == "end", textStreamPositioningParams);
            if (!pos.HasValue)
                throw new Exception(string.Format("Cannot detect the {0} of the log", boundName));
            return pos.Value;
        }

        private Task<TextStreamPosition> DetectEndPositionFromMediaSize()
        {
            return StreamTextAccess.StreamPositionToTextStreamPosition(mediaSize, StreamEncoding, VolatileStream, textStreamPositioningParams);
        }

        private async Task FindLogicalBounds(bool incrementalMode)
        {
            TextStreamPosition defaultBegin = new TextStreamPosition(0, TextStreamPosition.AlignMode.BeginningOfContainingBlock, textStreamPositioningParams);
            TextStreamPosition defaultEnd = await DetectEndPositionFromMediaSize();

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

        async Task<UpdateBoundsStatus> UpdateAvailableBoundsInternal(Ref<bool> incrementalMode)
        {
            await media.Update();

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
                incrementalMode.Value = false;
                oldMessagesAreInvalid = true;
            }

            await FindLogicalBounds(incrementalMode.Value);

            if (oldMessagesAreInvalid)
                return UpdateBoundsStatus.OldMessagesAreInvalid;

            return UpdateBoundsStatus.NewMessagesAvailable;
        }

        private ReadMessagesParams EnsureParserRangeDoesNotExceedReadersBoundaries(ReadMessagesParams p)
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
        readonly Lazy<StreamReadingStrategies.BaseStrategy> singleThreadedStrategy;
        readonly Lazy<StreamReadingStrategies.BaseStrategy> multiThreadedStrategy;
        readonly TextStreamPositioningParams textStreamPositioningParams;
        readonly bool isQuickFormatDetectionMode;
        readonly Settings.IGlobalSettingsAccessor settingsAccessor;
        readonly LJTraceSource trace;
        readonly Profiling.Counters perfCounters;

        Encoding encoding;

        long mediaSize;
        TextStreamPosition beginPosition;
        TextStreamPosition endPosition;
        ITimeOffsets timeOffsets = LogJoint.TimeOffsets.Empty;
        #endregion
    };

    internal class MessagesBuilderCallback : FieldsProcessor.IMessagesBuilderCallback
    {
        readonly ILogSourceThreadsInternal threads;
        readonly IThread fakeThread;
        long currentBeginPosition, currentEndPosition;
        StringSlice rawText;

        public MessagesBuilderCallback(ILogSourceThreadsInternal threads, IThread fakeThread)
        {
            this.threads = threads;
            this.fakeThread = fakeThread;
        }

        public long CurrentPosition => currentBeginPosition;

        public long CurrentEndPosition => currentEndPosition;

        public StringSlice CurrentRawText => rawText;

        public IThread GetThread(StringSlice id)
        {
            return fakeThread ?? threads.GetThread(id);
        }

        internal void SetCurrentPosition(long beginPosition, long endPosition)
        {
            currentBeginPosition = beginPosition;
            currentEndPosition = endPosition;
        }

        internal void SetRawText(StringSlice value)
        {
            rawText = value;
        }
    };
}
