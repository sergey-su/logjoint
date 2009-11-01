using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace LogJoint
{
	internal abstract class FileParsingLogReader : RangeManagingReader
	{
		public FileParsingLogReader(ILogReaderHost host, ILogReaderFactory factory, string fileName):
			base (host, factory)
		{
			this.fileName = fileName;
			this.stats.ConnectionParams["path"] = fileName;
		}

		public string FileName
		{
			get { return fileName; }
		}

		class StreamProvider : IPositionedMessagesProvider
		{
			FileParsingLogReader owner;
			MyFileStream stream;
			long endPosition;

			MyFileStream FStream
			{
				get
				{
					if (stream == null)
						stream = new MyFileStream(owner.FileName, owner.StreamGranularity);
					return stream;
				}
			}

			public StreamProvider(FileParsingLogReader owner)
			{
				this.owner = owner;
			}

			public void SetCurrentRange(FileRange.Range? range)
			{
				FStream.CurrentRange = range;
			}
			
			public long BeginPosition
			{
				get { return 0; }
			}

			public long EndPosition
			{
				get { return endPosition; }
			}

			public long Position
			{
				get
				{
					return FStream.Position;
				}
				set
				{
					FStream.Position = value;
				}
			}

			public long ActiveRangeRadius
			{
				get { return 1024 * 512; }
			}

			public void LocateDateLowerBound(DateTime d)
			{
				owner.LocateDateLowerBound(FStream, d, EndPosition);
			}

			public void LocateDateUpperBound(DateTime d)
			{
				owner.LocateDateUpperBound(FStream, d, EndPosition);
			}

			public bool UpdatEndPosition()
			{
				long tmp = FStream.Length;
				if (tmp == endPosition)
					return false;
				endPosition = tmp;
				owner.stats.TotalBytes = tmp;
				owner.AcceptStats(StatsFlag.BytesCount);
				return true;
			}

			class Reader : RangeManagingReader.IPositionedMessagesReader
			{
				FileParsingLogReader.IStreamParser parser;
				MyFileStream fso;

				public Reader(FileParsingLogReader.IStreamParser parser, FileRange.Range? range, MyFileStream fso)
				{
					this.parser = parser;
					this.fso = fso;
					fso.CurrentRange = range;
				}

				public MessageBase ReadNext()
				{
					return parser.ReadNext();
				}

				public long GetPositionOfNextMessage()
				{
					return parser.GetPositionOfNextMessage();
				}

				public long GetPositionBeforeNextMessage()
				{
					return parser.GetPositionBeforeNextMessage();
				}

				public void Dispose()
				{
					parser.Dispose();
					fso.CurrentRange = null;
				}
			};

			public RangeManagingReader.IPositionedMessagesReader CreateReader(FileRange.Range? range, bool isMainStreamReader)
			{
				return new Reader(owner.CreateParser(FStream, EndPosition, isMainStreamReader), range, FStream);
			}

			public bool UpdateAvailableBounds(bool incrementalMode)
			{
				long prevEndPosition = EndPosition;
				
				if (!UpdatEndPosition())
					return false;

				// The size of source file has reduces. This means that the 
				// file was probably overwritten. We have to delete all the messages 
				// we have loaded so far and start loading the file from the beginning.
				// Otherwise there is a high posiblity of messages' integrity violation.
				if (EndPosition < prevEndPosition)
				{
					owner.InvalidateEverythingThatHasBeenLoaded();
					incrementalMode = false;
				}

				return owner.UpdateAvailableBounds(FStream, EndPosition, incrementalMode);
			}

			public void Dispose()
			{	
				if (stream != null)
				{
					stream.Dispose();
				}
			}


			class MyFileStream : FileStream
			{
				FileRange.Range? currentRange;
				int granularity;

				public MyFileStream(string fileName, int granularity)
					: base(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
				{
					this.granularity = granularity;
				}

				public FileRange.Range? CurrentRange
				{
					get { return currentRange; }
					set { currentRange = value; }
				}

				public override int Read(byte[] array, int offset, int count)
				{
					if (granularity != 0)
						if (count > granularity)
							count = granularity;

					count = CheckEndPositionStopCondition(count);

					return base.Read(array, offset, count);
				}

				public override int ReadByte()
				{
					int count = CheckEndPositionStopCondition(1);

					int rv = -1;
					if (count == 1)
						rv = base.ReadByte();

					return rv;
				}

				int CheckEndPositionStopCondition(int count)
				{
					if (currentRange.HasValue)
					{
						long end = currentRange.Value.End;
						if (Position + count > end)
						{
							count = (int)(end - Position);
							if (count < 0)
								count = 0;
						}
					}

					return count;
				}
			};
		};

		protected override IPositionedMessagesProvider CreateProvider()
		{
			return new StreamProvider(this);
		}

		DateRange GetAvailableDateRange(Stream s, long endPosition, DateTime? knownBeginDate)
		{
			DateTime begin;
			if (!knownBeginDate.HasValue)
			{
				s.Position = 0;
				begin = ReadNearestDate(s, endPosition);
			}
			else
			{
				begin = knownBeginDate.Value;
			}
			if (begin == DateTime.MinValue)
				return new DateRange();

			long posStep = 6;
			long pos = endPosition - posStep;
			if (pos < 0)
				return DateRange.MakeFromBoundaryValues(begin, begin);

			for (; ; )
			{
				s.Position = pos;
				DateTime d = ReadNearestDate(s, endPosition);
				if (d != DateTime.MinValue)
					return DateRange.MakeFromBoundaryValues(begin, d);
				if (pos == 0)
					return DateRange.MakeFromBoundaryValues(begin, begin);
				pos -= posStep;
				if (pos < 0)
					pos = 0;
			}
		}

		bool UpdateAvailableBounds(Stream fso, long endPosition, bool incrementalMode)
		{
			DateTime? knownBegin = null;
			if (incrementalMode && stats.AvailableTime.HasValue)
				knownBegin = stats.AvailableTime.Value.Begin;

			DateRange tmp;

			try
			{
				tmp = GetAvailableDateRange(fso, endPosition, knownBegin);

			}
			catch (DateRangeArgumentException)
			{
				InvalidateEverythingThatHasBeenLoaded();
				tmp = GetAvailableDateRange(fso, endPosition, null);
				incrementalMode = false;
			}

			if (incrementalMode && tmp.Equals(stats.AvailableTime))
				return false;

			stats.AvailableTime = tmp;

			StatsFlag f = StatsFlag.AvailableTime;
			if (incrementalMode)
				f |= StatsFlag.AvailableTimeUpdatedIncrementallyFlag;
			AcceptStats(f);

			return true;
		}

		DateTime ReadNearestDate(Stream s, long endPosition)
		{
			using (IStreamParser parser = CreateParser(s, endPosition, false))
			{
				MessageBase l = parser.ReadNext();
				if (l != null)
					return l.Time;
			}
			return DateTime.MinValue;
		}

		bool LocateDateLowerBound(Stream s, DateTime d, long endPosition)
		{
			long initLen = endPosition;
			long count = initLen;
			long ret = 0;

			for (; 0 < count; )
			{
				long count2 = count / 2;
				s.Position = ret + count2;

				DateTime d2 = ReadNearestDate(s, endPosition);
				if (d2 < d)
				{
					ret += count2 + 1;
					count -= count2 + 1;
				}
				else
				{
					count = count2;
				}
			}

			if (ret == initLen)
				return false;

			s.Position = ret;
			return true;
		}

		bool LocateDateUpperBound(Stream s, DateTime d, long endPosition)
		{
			long initLen = endPosition;
			long count = initLen;
			long ret = 0;

			for (; 0 < count; )
			{
				long count2 = count / 2;
				s.Position = ret + count2;

				DateTime d2 = ReadNearestDate(s, endPosition);
				if (d2 <= d)
				{
					ret += count2 + 1;
					count -= count2 + 1;
				}
				else
				{
					count = count2;
				}
			}

			if (ret == initLen)
				return false;

			s.Position = ret;
			return true;
		}

		protected interface IStreamParser : IDisposable
		{
			MessageBase ReadNext();
			long GetPositionOfNextMessage();
			long GetPositionBeforeNextMessage();
		};
		protected abstract IStreamParser CreateParser(Stream s, long endPosition, bool isMainStreamParser);

		protected int StreamGranularity = 0;
		string fileName;
	};
}
