using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Tests
{
    public sealed class FakeMessagesReader : IMessagesReader
    {
        private readonly long[] positions;
        private bool isDisposed;
        private bool boundsUpdated;

        public FakeMessagesReader(long[] positions)
        {
            this.positions = positions;
        }

        public static readonly DateTime DateOrigin = new(2009, 1, 1);

        public static DateTime PositionToDate(long position)
        {
            return DateOrigin.AddSeconds(position);
        }

        long IMessagesReader.BeginPosition
        {
            get
            {
                CheckDisposed();
                return positions.Length > 0 ? positions[0] : 0;
            }
        }

        long IMessagesReader.EndPosition
        {
            get
            {
                CheckDisposed();
                return GetEndPosition();
            }
        }

        long IMessagesReader.SizeInBytes => 0xffff;

        Task<UpdateBoundsStatus> IMessagesReader.UpdateAvailableBounds(bool incrementalMode)
        {
            CheckDisposed();
            if (boundsUpdated)
            {
                return Task.FromResult(UpdateBoundsStatus.NothingUpdated);
            }
            boundsUpdated = true;
            return Task.FromResult(UpdateBoundsStatus.NewMessagesAvailable);
        }

        long IMessagesReader.MaximumMessageSize => 0;

        ITimeOffsets IMessagesReader.TimeOffsets
        {
            get { return LogJoint.TimeOffsets.Empty; }
            set { }
        }

        long IMessagesReader.PositionRangeToBytes(LogJoint.FileRange.Range range)
        {
            CheckDisposed();
            return range.Length;
        }

        ValueTask<int> IMessagesReader.GetContentsEtag()
        {
            CheckDisposed();
            return new ValueTask<int>(0);
        }

        IAsyncEnumerable<PostprocessedMessage> IMessagesReader.Read(ReadMessagesParams p)
        {
            return Read(p).ToAsyncEnumerable();
        }


        IAsyncEnumerable<SearchResultMessage> IMessagesReader.Search(SearchMessagesParams p)
        {
            return Search(p).ToAsyncEnumerable();
        }

        Encoding IMessagesReader.Encoding => Encoding.ASCII;

        void IDisposable.Dispose()
        {
            isDisposed = true;
        }

        void CheckDisposed()
        {
            if (!isDisposed)
            {
                return;
            }
            throw new ObjectDisposedException(this.ToString());
        }

        long GetEndPosition() => positions.Length > 0 ? (positions[positions.Length - 1] + 1) : 0;

        IEnumerable<PostprocessedMessage> Read(ReadMessagesParams p)
        {
            CheckDisposed();

            long positionIndex = 0;

            if (p.Direction == ReadMessagesDirection.Forward)
            {
                for (positionIndex = 0; positionIndex < positions.Length; ++positionIndex)
                {
                    if (positions[positionIndex] >= p.StartPosition)
                        break;
                }
            }
            else
            {
                long currentEnd = GetEndPosition();
                for (positionIndex = positions.Length - 1; positionIndex >= 0; --positionIndex)
                {
                    if (currentEnd <= p.StartPosition)
                        break;
                    currentEnd = positions[positionIndex];
                }
            }

            for (; ; )
            {
                CheckDisposed();

                long currPos;
                if (p.Direction == ReadMessagesDirection.Forward)
                {
                    if (positionIndex >= positions.Length)
                        break;

                    currPos = positions[positionIndex];
                    if (p.Range.HasValue && currPos >= p.Range.Value.End)
                        break;

                    ++positionIndex;
                }
                else
                {
                    if (positionIndex < 0)
                        break;

                    currPos = positions[positionIndex];
                    if (p.Range.HasValue && currPos < p.Range.Value.Begin)
                        break;

                    --positionIndex;
                }

                IMessage m = new Message(currPos, currPos + 1, null,
                    new MessageTimestamp(PositionToDate(currPos)), new StringSlice(currPos.ToString()), SeverityFlag.Info,
                    rawText: new StringSlice($"{currPos}.r"));
                yield return new PostprocessedMessage(m, null);
            }
        }

        IEnumerable<SearchResultMessage> Search(SearchMessagesParams p)
        {
            if (p.ContinuationToken != null)
                throw new NotImplementedException("Fake messages reader does not support search continuation");
            using var processing = p.SearchParams.Filters.StartBulkProcessing(
                MessageTextGetters.Get(p.SearchParams.SearchInRawText), reverseMatchDirection: false);
            foreach (PostprocessedMessage msg in Read(new ReadMessagesParams()
            {
                StartPosition = p.SearchParams.FromPosition.GetValueOrDefault(0),
                Range = p.Range,
            }))
            {
                MessageFilteringResult filteringResult = processing.ProcessMessage(msg.Message, startFromChar: null);
                if (filteringResult.Action != FilterAction.Exclude)
                    yield return new SearchResultMessage(msg.Message, filteringResult);
            }
        }
    };
}
