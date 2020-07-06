using LogJoint.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
	class DelegatingMessagesReader: IPositionedMessagesReader
	{
		public DelegatingMessagesReader(IPositionedMessagesReader underliyingReader)
		{
			if (underliyingReader == null)
				throw new ArgumentNullException("underliyingReader");
			this.underliyingReader = underliyingReader;
		}

		#region IPositionedMessagesReader Members

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

		public Task<IPositionedMessagesParser> CreateParser(CreateParserParams p)
		{
			return underliyingReader.CreateParser(p);
		}

		public ISearchingParser CreateSearchingParser(CreateSearchingParserParams p)
		{
			return underliyingReader.CreateSearchingParser(p);
		}

		int IPositionedMessagesReader.GetContentsEtag()
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

		protected readonly IPositionedMessagesReader underliyingReader;
	}
}
