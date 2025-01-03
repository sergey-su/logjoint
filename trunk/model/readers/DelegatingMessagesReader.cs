using LogJoint.Postprocessing;
using LogJoint.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
    class DelegatingMessagesReader : IMessagesReader
    {
        public DelegatingMessagesReader(IMessagesReader underliyingReader)
        {
            if (underliyingReader == null)
                throw new ArgumentNullException("underliyingReader");
            this.underliyingReader = underliyingReader;
        }

        #region IMessagesReader Members

        public long BeginPosition
        {
            get { return underliyingReader.BeginPosition; }
        }

        public long EndPosition
        {
            get { return underliyingReader.EndPosition; }
        }

        public Task<UpdateBoundsStatus> UpdateAvailableBounds(bool incrementalMode)
        {
            return underliyingReader.UpdateAvailableBounds(incrementalMode);
        }

        public long MaximumMessageSize
        {
            get { return underliyingReader.MaximumMessageSize; }
        }

        public long PositionRangeToBytes(LogJoint.FileRange.Range range)
        {
            return underliyingReader.PositionRangeToBytes(range);
        }

        public long SizeInBytes
        {
            get { return underliyingReader.SizeInBytes; }
        }

        public ITimeOffsets TimeOffsets
        {
            get { return underliyingReader.TimeOffsets; }
            set { underliyingReader.TimeOffsets = value; }
        }

        public IAsyncEnumerable<PostprocessedMessage> Read(ReadMessagesParams p)
        {
            return underliyingReader.Read(p);
        }

        public IAsyncEnumerable<SearchResultMessage> Search(SearchMessagesParams p)
        {
            return underliyingReader.Search(p);
        }

        ValueTask<int> IMessagesReader.GetContentsEtag()
        {
            return underliyingReader.GetContentsEtag();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            underliyingReader.Dispose();
        }

        #endregion

        protected readonly IMessagesReader underliyingReader;
    }
}
