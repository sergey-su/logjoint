using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.RegularExpressions;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace LogJoint.StreamParsingStrategies
{
	public abstract class MultiThreadedStrategy<UserThreadLocalData> : BaseStrategy
	{
		public MultiThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, MessagesSplitterFlags splitterFlags)
			: this(media, encoding, headerRe, splitterFlags, false)
		{
		}

		public abstract MessageBase MakeMessage(TextMessageCapture capture, UserThreadLocalData threadLocal);
		public abstract UserThreadLocalData InitializeThreadLocalState();

		public const int BytesToParsePerThread = 1024 * 1024;

		#region BaseStrategy overrides

		public override MessageBase ReadNext()
		{
			if (!attachedToParser)
				throw new InvalidOperationException("Cannot read messages when not attached to a parser");
			if (enumer == null)
				enumer = MessagesEnumerator().GetEnumerator();
			if (!enumer.MoveNext())
				return null;
			return enumer.Current;
		}

		public override void ParserCreated(CreateParserParams p)
		{
			attachedToParser = true;
			currentParams = p;
			base.ParserCreated(p);
		}

		public override void ParserDestroyed()
		{
			if (enumer != null)
			{
				enumer.Dispose();
				enumer = null;
			}
			attachedToParser = false;
			streamDataPool.Clear();
			outputBuffersPool.Clear();
			base.ParserDestroyed();
		}
		#endregion

		#region Internal members that can be accessed from unit tests
		
		internal MultiThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, MessagesSplitterFlags splitterFlags, bool useMockThreading)
			: base(media, encoding, headerRe)
		{
			this.streamDataPool = new ThreadSafeObjectPool<Byte[]>(pool => new Byte[BytesToParsePerThread]);
			this.outputBuffersPool = new ThreadSafeObjectPool<List<MessageBase>>(pool => new List<MessageBase>(1024 * 8));
			this.useMockThreading = useMockThreading;
			this.splitterFlags = splitterFlags;
		}

		internal ISequentialMediaReaderAndProcessorMock MockedReaderAndProcessor
		{
			get { return mockedReaderAndProcessor; }
		}

		#endregion

		#region Implementation

		static MultiThreadedStrategy()
		{
			Debug.Assert((BytesToParsePerThread % TextStreamPosition.AlignmentBlockSize) == 0);
		}

		struct StreamData
		{
			public readonly long Position;
			public readonly byte[] Bytes;
			public readonly int Length;
			public StreamData(long position, byte[] bytes, int len)
			{
				this.Position = position;
				this.Bytes = bytes;
				this.Length = len;
			}
			public StreamData(long position, byte[] bytes)
				: this(position, bytes, bytes.Length)
			{
			}
			public bool IsEmpty
			{
				get { return Bytes == null; }
			}
			public MemoryStream ToMemoryStream()
			{
				return new MemoryStream(Bytes, 0, Length, false);
			}
		};

		class PieceOfWork
		{
			public StreamData prevStreamData;
			public StreamData streamData;
			public StreamData nextStreamData;

			public long startTextPosition;
			public long stopTextPosition;

			public List<MessageBase> outputBuffer;

			public override string ToString()
			{
				return string.Format("{0}-{1}", startTextPosition, stopTextPosition);
			}
		};

		struct ThreadLocalData
		{
			public IRegex headRe;
			public GeneratingStream paddingStream;
			public ConcatReadingStream stream;
			public StreamTextAccess textAccess;
			public IMessagesSplitter splitter;
			public TextMessageCapture capture;
			public UserThreadLocalData userData;
		};

		class Callback: SequentialMediaReaderAndProcessor<PieceOfWork, PieceOfWork, ThreadLocalData>.ICallback
		{
			readonly MultiThreadedStrategy<UserThreadLocalData> owner;

			public Callback(MultiThreadedStrategy<UserThreadLocalData> owner)
			{
				this.owner = owner;
			}

			public IEnumerable<PieceOfWork> ReadRawDataFromMedia(CancellationToken cancellationToken)
			{
				if (owner.currentParams.Direction == MessagesParserDirection.Forward)
					return ReadRawDataFromMedia_Forward();
				else
					return ReadRawDataFromMedia_Backward();
			}

			IEnumerable<PieceOfWork> ReadRawDataFromMedia_Backward()
			{
				Stream stream = owner.media.DataStream;
				CreateParserParams parserParams = owner.currentParams;
				FileRange.Range range = parserParams.Range.Value;
				TextStreamPosition startPosition = new TextStreamPosition(parserParams.StartPosition);

				long beginStreamPos = new TextStreamPosition(range.Begin).StreamPositionAlignedToBlockSize;
				long endStreamPos = startPosition.StreamPositionAlignedToBlockSize + TextStreamPosition.AlignmentBlockSize;

				if (beginStreamPos != 0 && !owner.encoding.IsSingleByte)
				{
					int maxBytesPerCharacter = owner.encoding.GetMaxByteCount(1);
					beginStreamPos -= maxBytesPerCharacter;
				}
				
				PieceOfWork firstPieceOfWork = new PieceOfWork();

				{
					firstPieceOfWork.streamData = AllocateAndReadStreamData_Backward(stream, endStreamPos);
					if (firstPieceOfWork.streamData.IsEmpty)
						yield break;
					firstPieceOfWork.startTextPosition = startPosition.Value;
					firstPieceOfWork.stopTextPosition = endStreamPos - BytesToParsePerThread;
					firstPieceOfWork.outputBuffer = owner.outputBuffersPool.LockAndGet();
					endStreamPos -= BytesToParsePerThread;
				}

				PieceOfWork pieceOfWorkToYieldNextTime = firstPieceOfWork;

				for (; ; )
				{
					PieceOfWork nextPieceOfWork = new PieceOfWork();
					nextPieceOfWork.streamData = AllocateAndReadStreamData_Backward(stream, endStreamPos);
					nextPieceOfWork.nextStreamData = pieceOfWorkToYieldNextTime.streamData;
					nextPieceOfWork.startTextPosition = endStreamPos;
					nextPieceOfWork.stopTextPosition = endStreamPos - BytesToParsePerThread;
					nextPieceOfWork.outputBuffer = owner.outputBuffersPool.LockAndGet();

					pieceOfWorkToYieldNextTime.prevStreamData = nextPieceOfWork.streamData;

					yield return pieceOfWorkToYieldNextTime;

					if (endStreamPos < beginStreamPos)
						break;
					if (nextPieceOfWork.streamData.IsEmpty)
						break;

					pieceOfWorkToYieldNextTime = nextPieceOfWork;
					endStreamPos -= BytesToParsePerThread;
				}
			}

			IEnumerable<PieceOfWork> ReadRawDataFromMedia_Forward()
			{
				Stream stream = owner.media.DataStream;
				CreateParserParams parserParams = owner.currentParams;
				FileRange.Range range = parserParams.Range.Value;
				TextStreamPosition startPosition = new TextStreamPosition(parserParams.StartPosition);

				long beginStreamPos = startPosition.StreamPositionAlignedToBlockSize;
				long endStreamPos = new TextStreamPosition(range.End).StreamPositionAlignedToBlockSize + TextStreamPosition.AlignmentBlockSize;

				PieceOfWork firstPieceOfWork = new PieceOfWork();

				if (beginStreamPos != 0 && !owner.encoding.IsSingleByte)
				{
					int maxBytesPerCharacter = owner.encoding.GetMaxByteCount(1);
					firstPieceOfWork.prevStreamData = new StreamData(
						beginStreamPos - maxBytesPerCharacter, new byte[maxBytesPerCharacter]);
					stream.Position = beginStreamPos - maxBytesPerCharacter;
					stream.Read(firstPieceOfWork.prevStreamData.Bytes, 0, maxBytesPerCharacter);
				}
				else
				{
					stream.Position = beginStreamPos;
				}

				{
					firstPieceOfWork.streamData = AllocateAndReadStreamData(stream);
					if (firstPieceOfWork.streamData.IsEmpty)
						yield break;
					firstPieceOfWork.startTextPosition = startPosition.Value;
					firstPieceOfWork.stopTextPosition = beginStreamPos + BytesToParsePerThread;
					firstPieceOfWork.outputBuffer = owner.outputBuffersPool.LockAndGet();
					beginStreamPos += BytesToParsePerThread;
				}

				PieceOfWork pieceOfWorkToYieldNextTime = firstPieceOfWork;

				for (; ; )
				{
					PieceOfWork nextPieceOfWork = new PieceOfWork();
					nextPieceOfWork.streamData = AllocateAndReadStreamData(stream);
					nextPieceOfWork.prevStreamData = pieceOfWorkToYieldNextTime.streamData;
					nextPieceOfWork.startTextPosition = beginStreamPos;
					nextPieceOfWork.stopTextPosition = beginStreamPos + BytesToParsePerThread;
					nextPieceOfWork.outputBuffer = owner.outputBuffersPool.LockAndGet();

					pieceOfWorkToYieldNextTime.nextStreamData = nextPieceOfWork.streamData;

					yield return pieceOfWorkToYieldNextTime;

					if (beginStreamPos > endStreamPos)
						break;
					if (nextPieceOfWork.streamData.IsEmpty)
						break;

					pieceOfWorkToYieldNextTime = nextPieceOfWork;
					beginStreamPos += BytesToParsePerThread;
				}
			}


			StreamData AllocateAndReadStreamData(Stream readFrom)
			{
				var bytes = owner.streamDataPool.LockAndGet();
				long position = readFrom.Position;
				int read = readFrom.Read(bytes, 0, bytes.Length);
				if (read == 0)
				{
					owner.streamDataPool.Release(bytes);
					return new StreamData();
				}
				return new StreamData(position, bytes, read);
			}

			StreamData AllocateAndReadStreamData_Backward(Stream readFrom, long streamPositionToReadTill)
			{
				if (streamPositionToReadTill <= 0)
					return new StreamData();
				var bytes = owner.streamDataPool.LockAndGet();
				long streamPositionToReadFrom = Math.Max(streamPositionToReadTill - bytes.Length, 0);
				readFrom.Position = streamPositionToReadFrom;
				int read = readFrom.Read(bytes, 0, (int)(streamPositionToReadTill - streamPositionToReadFrom));
				if (read == 0)
				{
					owner.streamDataPool.Release(bytes);
					return new StreamData();
				}
				return new StreamData(streamPositionToReadFrom, bytes, read);
			}

			public ThreadLocalData InitializeThreadLocalState()
			{
				ThreadLocalData tld = new ThreadLocalData();
				tld.headRe = owner.headerRe.Factory.Create(owner.headerRe.Pattern, owner.headerRe.Options);
				tld.stream = new ConcatReadingStream();
				tld.textAccess = new StreamTextAccess(tld.stream, owner.encoding);
				tld.splitter = new ReadMessageFromTheMiddleProblem(new MessagesSplitter(tld.textAccess, tld.headRe, owner.splitterFlags));
				tld.paddingStream = new GeneratingStream(0, 0);
				tld.capture = new TextMessageCapture();
				tld.userData = owner.InitializeThreadLocalState();
				return tld;
			}

			public PieceOfWork ProcessRawData(PieceOfWork pieceOfWork, ThreadLocalData tls, CancellationToken cancellationToken)
			{			
				var stms = new List<Stream>();
 				stms.Add(tls.paddingStream);
				if (!pieceOfWork.prevStreamData.IsEmpty)
					stms.Add(pieceOfWork.prevStreamData.ToMemoryStream());
				stms.Add(pieceOfWork.streamData.ToMemoryStream());
				if (!pieceOfWork.nextStreamData.IsEmpty)
					stms.Add(pieceOfWork.nextStreamData.ToMemoryStream());

				long paddingStreamSize;
				if (!pieceOfWork.prevStreamData.IsEmpty)
					paddingStreamSize = pieceOfWork.prevStreamData.Position;
				else
					paddingStreamSize = pieceOfWork.streamData.Position;
				tls.paddingStream.SetLength(paddingStreamSize);

				tls.stream.Update(stms);

				var direction = owner.currentParams.Direction;

				tls.splitter.BeginSplittingSession(
					owner.currentParams.Range.Value, 
					pieceOfWork.startTextPosition, 
					direction);
				for (; ; )
				{
					if (cancellationToken.IsCancellationRequested)
						break;
					if (!tls.splitter.GetCurrentMessageAndMoveToNextOne(tls.capture))
						break;
					bool stopPositionReached = direction == MessagesParserDirection.Forward ?
						tls.capture.BeginPosition >= pieceOfWork.stopTextPosition : tls.capture.EndPosition <= pieceOfWork.stopTextPosition;
					if (stopPositionReached)
						break;
					var x = owner.MakeMessage(tls.capture, tls.userData);
					if (x == null)
						break;
					pieceOfWork.outputBuffer.Add(x);
				}
				tls.splitter.EndSplittingSession();

				return pieceOfWork;
			}
		};

		IEnumerable<MessageBase> MessagesEnumerator()
		{
			var readerAndProcessorCallback = new Callback(this);

			ISequentialMediaReaderAndProcessor<PieceOfWork> readerAndProcessor;

			if (!useMockThreading)
			{
				readerAndProcessor = new SequentialMediaReaderAndProcessor<PieceOfWork, PieceOfWork, ThreadLocalData>(readerAndProcessorCallback);
			}
			else 
			{
				var mockedReaderAndProcessorImpl = new SequentialMediaReaderAndProcessorMock<PieceOfWork, PieceOfWork, ThreadLocalData>(readerAndProcessorCallback);
				mockedReaderAndProcessor = mockedReaderAndProcessorImpl;
				readerAndProcessor = mockedReaderAndProcessorImpl;
			}

			using (readerAndProcessor)
			{
				for (; ; )
				{
					PieceOfWork currentPieceOfWork = readerAndProcessor.ReadAndProcessNextPieceOfData();
					if (currentPieceOfWork == null)
						break;

					// Here is tricky: returning bytes buffer of the the piece of work that was handled previously.
					// Bytes buffer of current piece (currentPieceOfWork.streamData) can still be used
					// by a thread processing the piece following the current one.
					if (currentParams.Direction	== MessagesParserDirection.Forward)
						SafeReturnStreamDataToThePool(currentPieceOfWork.prevStreamData);
					else
						SafeReturnStreamDataToThePool(currentPieceOfWork.nextStreamData);

					foreach (var m in currentPieceOfWork.outputBuffer)
					{
						yield return m;
					}

					ReturnOutputBufferToThePool(currentPieceOfWork.outputBuffer);
				}
			}
		}

		void SafeReturnStreamDataToThePool(StreamData streamData)
		{
			if (!streamData.IsEmpty && streamData.Bytes.Length == BytesToParsePerThread)
				streamDataPool.Release(streamData.Bytes);
		}

		void ReturnOutputBufferToThePool(List<MessageBase> buffer)
		{
			buffer.Clear();
			outputBuffersPool.Release(buffer);
		}


		readonly ThreadSafeObjectPool<Byte[]> streamDataPool;
		readonly ThreadSafeObjectPool<List<MessageBase>> outputBuffersPool;
		readonly MessagesSplitterFlags splitterFlags;
		readonly bool useMockThreading;

		ISequentialMediaReaderAndProcessorMock mockedReaderAndProcessor;
		IEnumerator<MessageBase> enumer;
		CreateParserParams currentParams;
		bool attachedToParser;

		#endregion
	}
}
