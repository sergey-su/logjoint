using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogJoint.StreamParsingStrategies
{
	public abstract class MultiThreadedStrategy<UserThreadLocalData> : BaseStrategy
	{
		public MultiThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, 
			MessagesSplitterFlags splitterFlags, TextStreamPositioningParams textStreamPositioningParams,
			string parentLoggingPrefix, ITraceSourceFactory traceSourceFactory)
			: this(media, encoding, headerRe, splitterFlags, false, textStreamPositioningParams, parentLoggingPrefix, traceSourceFactory)
		{
			BytesToParsePerThread = GetBytesToParsePerThread(textStreamPositioningParams);
		}

		public abstract IMessage MakeMessage(TextMessageCapture capture, UserThreadLocalData threadLocal);
		public abstract UserThreadLocalData InitializeThreadLocalState();

		public readonly int BytesToParsePerThread;
		public const int DefaultBytesToParsePerThread = TextStreamPositioningParams.DefaultAlignmentBlockSize * 2;

		public static int GetBytesToParsePerThread(TextStreamPositioningParams textStreamPositioningParams)
		{
			if (textStreamPositioningParams.AlignmentBlockSize > DefaultBytesToParsePerThread)
				return textStreamPositioningParams.AlignmentBlockSize;
			Debug.Assert((DefaultBytesToParsePerThread % textStreamPositioningParams.AlignmentBlockSize) == 0);
			return DefaultBytesToParsePerThread;
		}

		#region BaseStrategy overrides

		public override async ValueTask<IMessage> ReadNext()
		{
			return (await ReadNextAndPostprocess()).Message;
		}

		public override ValueTask<PostprocessedMessage> ReadNextAndPostprocess()
		{
			if (!attachedToParser)
				throw new InvalidOperationException("Cannot read messages when not attached to a parser");
			if (enumer == null)
				enumer = MessagesEnumerator().GetEnumerator();
			if (!enumer.MoveNext())
				return new ValueTask<PostprocessedMessage>(new PostprocessedMessage());
			return new ValueTask<PostprocessedMessage>(enumer.Current);
		}

		public override Task ParserCreated(CreateParserParams p)
		{
			tracer.Info("Parser created");
			attachedToParser = true;
			currentParams = p;
			return base.ParserCreated(p);
		}

		public override void ParserDestroyed()
		{
			tracer.Info("Parser destroyed");
			try
			{
				enumer?.Dispose(); // enumerator may throw here if AsParallel failed
			}
			finally
			{
				enumer = null;
				attachedToParser = false;
				streamDataPool.Clear();
				outputBuffersPool.Clear();
				base.ParserDestroyed();
			}
		}
		#endregion

		#region Internal members that can be accessed from unit tests
		
		internal MultiThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, MessagesSplitterFlags splitterFlags, 
			bool useMockThreading, TextStreamPositioningParams textStreamPositioningParams, string parentLoggingPrefix, ITraceSourceFactory traceSourceFactory)
			: base(media, encoding, headerRe, textStreamPositioningParams)
		{
			if (parentLoggingPrefix != null)
			{
				this.tracer = traceSourceFactory.CreateTraceSource("LogSource", string.Format("{0}.mts_{1:x4}", parentLoggingPrefix, Hashing.GetShortHashCode(this.GetHashCode())));
			}
			this.streamDataPool = new ThreadSafeObjectPool<Byte[]>(pool =>
			{
				var ret = new Byte[BytesToParsePerThread];
				tracer.Info("Allocating new piece of stream data: {0:x8}", ret.GetHashCode());
				return ret;
			});
			this.outputBuffersPool = new ThreadSafeObjectPool<List<PostprocessedMessage>>(pool =>
			{
				var ret = new List<PostprocessedMessage>(1024 * 8);
				tracer.Info("Allocating new output buffer: {0:x8}", ret.GetHashCode());
				return ret;
			});
			this.useMockThreading = useMockThreading;
			this.splitterFlags = splitterFlags;
		}

		internal ISequentialMediaReaderAndProcessorMock MockedReaderAndProcessor
		{
			get { return mockedReaderAndProcessor; }
		}

		#endregion

		#region Implementation

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

		class PieceOfWork: IDisposable
		{
			public readonly int id;
			public StreamData prevStreamData;
			public StreamData streamData;
			public StreamData nextStreamData;

			public long startTextPosition;
			public long stopTextPosition;

			public List<PostprocessedMessage> outputBuffer;

			public Profiling.Operation perfop;

			public PieceOfWork(int id, LJTraceSource trace)
			{
				this.id = id;
				this.perfop = new Profiling.Operation(trace, string.Format("#{0}", id));
			}

			public void Dispose()
			{
				this.perfop.Dispose();
			}

			public override string ToString()
			{
				return string.Format("{2}: {0}-{1}", startTextPosition, stopTextPosition, id);
			}
		};

		struct ThreadLocalData
		{
			public int id;
			public IRegex headRe;
			public GeneratingStream paddingStream;
			public ConcatReadingStream stream;
			public StreamTextAccess textAccess;
			public IMessagesSplitter splitter;
			public TextMessageCapture capture;
			public IMessagesPostprocessor postprocessor;
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
					return ReadRawDataFromMedia_Forward(cancellationToken);
				else
					return ReadRawDataFromMedia_Backward(cancellationToken);
			}

			IEnumerable<PieceOfWork> ReadRawDataFromMedia_Backward(CancellationToken cancellationToken)
			{
				Stream stream = owner.media.DataStream;
				CreateParserParams parserParams = owner.currentParams;
				FileRange.Range range = parserParams.Range.Value;
				TextStreamPosition startPosition = new TextStreamPosition(parserParams.StartPosition, owner.textStreamPositioningParams);

				long beginStreamPos = new TextStreamPosition(range.Begin, owner.textStreamPositioningParams).StreamPositionAlignedToBlockSize;
				long endStreamPos = startPosition.StreamPositionAlignedToBlockSize + owner.textStreamPositioningParams.AlignmentBlockSize;

				if (beginStreamPos != 0 && !owner.encoding.IsSingleByte)
				{
					int maxBytesPerCharacter = owner.encoding.GetMaxByteCount(1);
					beginStreamPos -= maxBytesPerCharacter;
				}
				
				PieceOfWork firstPieceOfWork = new PieceOfWork(Interlocked.Increment(ref owner.nextPieceOfWorkId), owner.tracer);

				{
					firstPieceOfWork.streamData = AllocateAndReadStreamData_Backward(stream, endStreamPos);
					if (firstPieceOfWork.streamData.IsEmpty)
						yield break;
					firstPieceOfWork.startTextPosition = startPosition.Value;
					firstPieceOfWork.stopTextPosition = endStreamPos - owner.BytesToParsePerThread;
					firstPieceOfWork.outputBuffer = owner.AllocateOutputBuffer();
					endStreamPos -= owner.BytesToParsePerThread;
				}

				PieceOfWork pieceOfWorkToYieldNextTime = firstPieceOfWork;

				for (; ; )
				{
					cancellationToken.ThrowIfCancellationRequested();
					PieceOfWork nextPieceOfWork = new PieceOfWork(Interlocked.Increment(ref owner.nextPieceOfWorkId), owner.tracer);
					nextPieceOfWork.streamData = AllocateAndReadStreamData_Backward(stream, endStreamPos);
					nextPieceOfWork.nextStreamData = pieceOfWorkToYieldNextTime.streamData;
					nextPieceOfWork.startTextPosition = endStreamPos;
					nextPieceOfWork.stopTextPosition = endStreamPos - owner.BytesToParsePerThread;
					nextPieceOfWork.outputBuffer = owner.AllocateOutputBuffer();

					pieceOfWorkToYieldNextTime.prevStreamData = nextPieceOfWork.streamData;

					yield return pieceOfWorkToYieldNextTime;

					if (endStreamPos < beginStreamPos)
						break;
					if (nextPieceOfWork.streamData.IsEmpty)
						break;

					pieceOfWorkToYieldNextTime = nextPieceOfWork;
					endStreamPos -= owner.BytesToParsePerThread;
				}
			}

			IEnumerable<PieceOfWork> ReadRawDataFromMedia_Forward(CancellationToken cancellationToken)
			{
				Stream stream = owner.media.DataStream;
				CreateParserParams parserParams = owner.currentParams;
				FileRange.Range range = parserParams.Range.Value;
				TextStreamPosition startPosition = new TextStreamPosition(parserParams.StartPosition, owner.textStreamPositioningParams);

				long beginStreamPos = startPosition.StreamPositionAlignedToBlockSize;
				long endStreamPos = new TextStreamPosition(range.End, owner.textStreamPositioningParams).StreamPositionAlignedToBlockSize + owner.textStreamPositioningParams.AlignmentBlockSize;

				PieceOfWork firstPieceOfWork = new PieceOfWork(Interlocked.Increment(ref owner.nextPieceOfWorkId), owner.tracer);

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
					firstPieceOfWork.stopTextPosition = beginStreamPos + owner.BytesToParsePerThread;
					firstPieceOfWork.outputBuffer = owner.AllocateOutputBuffer();
					beginStreamPos += owner.BytesToParsePerThread;
				}

				PieceOfWork pieceOfWorkToYieldNextTime = firstPieceOfWork;

				for (; ; )
				{
					cancellationToken.ThrowIfCancellationRequested();
					PieceOfWork nextPieceOfWork = new PieceOfWork(Interlocked.Increment(ref owner.nextPieceOfWorkId), owner.tracer);
					nextPieceOfWork.streamData = AllocateAndReadStreamData(stream);
					nextPieceOfWork.prevStreamData = pieceOfWorkToYieldNextTime.streamData;
					nextPieceOfWork.startTextPosition = beginStreamPos;
					nextPieceOfWork.stopTextPosition = beginStreamPos + owner.BytesToParsePerThread;
					nextPieceOfWork.outputBuffer = owner.AllocateOutputBuffer();

					pieceOfWorkToYieldNextTime.nextStreamData = nextPieceOfWork.streamData;

					owner.tracer.Info("Start processing new peice of work. Currently being processed: {0}", Interlocked.Increment(ref owner.peicesOfWorkBeingProgressed));
					yield return pieceOfWorkToYieldNextTime;

					if (beginStreamPos > endStreamPos)
						break;
					if (nextPieceOfWork.streamData.IsEmpty)
						break;

					pieceOfWorkToYieldNextTime = nextPieceOfWork;
					beginStreamPos += owner.BytesToParsePerThread;
				}
			}


			StreamData AllocateAndReadStreamData(Stream readFrom)
			{
				var bytes = owner.AllocateStreamData();
				long position = readFrom.Position;
				int read = readFrom.Read(bytes, 0, bytes.Length);
				if (read == 0)
				{
					owner.tracer.Info("Releasing piece of stream data without using: {0:x8}", bytes.GetHashCode());
					owner.streamDataPool.Release(bytes);
					owner.tracer.Info("Stream data pool size: {0}", owner.streamDataPool.FreeObjectsCount);
					return new StreamData();
				}
				return new StreamData(position, bytes, read);
			}

			StreamData AllocateAndReadStreamData_Backward(Stream readFrom, long streamPositionToReadTill)
			{
				if (streamPositionToReadTill <= 0)
					return new StreamData();
				var bytes = owner.AllocateStreamData();
				long streamPositionToReadFrom = Math.Max(streamPositionToReadTill - bytes.Length, 0);
				readFrom.Position = streamPositionToReadFrom;
				int read = readFrom.Read(bytes, 0, (int)(streamPositionToReadTill - streamPositionToReadFrom));
				if (read == 0)
				{
					owner.tracer.Info("Releasing piece of stream data without using: {0:x8}", bytes.GetHashCode());
					owner.streamDataPool.Release(bytes);
					owner.tracer.Info("Stream data pool size: {0}", owner.streamDataPool.FreeObjectsCount);
					return new StreamData();
				}
				return new StreamData(streamPositionToReadFrom, bytes, read);
			}

			public ThreadLocalData InitializeThreadLocalState()
			{
				ThreadLocalData tld = new ThreadLocalData();
				tld.id = Interlocked.Increment(ref owner.lastThreadLocalStateId);
				tld.headRe = owner.headerRe.Factory.Create(owner.headerRe.Pattern, owner.headerRe.Options);
				tld.stream = new ConcatReadingStream();
				tld.textAccess = new StreamTextAccess(tld.stream, owner.encoding, owner.textStreamPositioningParams);
				tld.splitter = new ReadMessageFromTheMiddleProblem(new MessagesSplitter(tld.textAccess, tld.headRe, owner.splitterFlags));
				tld.paddingStream = new GeneratingStream(0, 0);
				tld.capture = new TextMessageCapture();
				tld.postprocessor = owner.currentParams.PostprocessorsFactory?.Invoke();
				tld.userData = owner.InitializeThreadLocalState();
				owner.tracer.Info("Initialized thread local state #{0}", tld.id);
				return tld;
			}

			public void FinalizeThreadLocalState(ref ThreadLocalData state)
			{
				int id = state.id;
				state.postprocessor?.Dispose();
				state = new ThreadLocalData();
				owner.tracer.Info("Finalized thread local state #{0}", id);
			}

			public PieceOfWork ProcessRawData(PieceOfWork pieceOfWork, ThreadLocalData tls, CancellationToken cancellationToken)
			{
				pieceOfWork.perfop.Milestone("Starting processing");

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
				var postprocessor = tls.postprocessor;

				tls.splitter.BeginSplittingSession(
					owner.currentParams.Range.Value, 
					pieceOfWork.startTextPosition, 
					direction).Wait(); // Wait() could be a problem in blazor, but blazor won't use multi-threaded strategy
				for (; ; )
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (!tls.splitter.GetCurrentMessageAndMoveToNextOne(tls.capture).Result) // Result - see comment above for Wait()
						break;
					bool stopPositionReached = direction == MessagesParserDirection.Forward ?
						tls.capture.BeginPosition >= pieceOfWork.stopTextPosition : tls.capture.EndPosition <= pieceOfWork.stopTextPosition;
					if (stopPositionReached)
						break;
					var x = owner.MakeMessage(tls.capture, tls.userData);
					if (x == null)
						break;
					var postprocessorResult = postprocessor?.Postprocess(x);
					pieceOfWork.outputBuffer.Add(new PostprocessedMessage(x, postprocessorResult));
				}
				tls.splitter.EndSplittingSession();

				pieceOfWork.perfop.Milestone("Finished processing");

				return pieceOfWork;
			}
		};

		IEnumerable<PostprocessedMessage> MessagesEnumerator()
		{
			tracer.Info("Enumerator entered");

			var readerAndProcessorCallback = new Callback(this);

			ISequentialMediaReaderAndProcessor<PieceOfWork> readerAndProcessor;

			if (!useMockThreading)
			{
				readerAndProcessor = new SequentialMediaReaderAndProcessor<PieceOfWork, PieceOfWork, ThreadLocalData>(readerAndProcessorCallback, currentParams.Cancellation);
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
					currentParams.Cancellation.ThrowIfCancellationRequested();
					PieceOfWork currentPieceOfWork = readerAndProcessor.ReadAndProcessNextPieceOfData();
					if (currentPieceOfWork == null)
						break;
					currentPieceOfWork.perfop.Milestone("Starting consuming");
					tracer.Info("Messages in output buffer: {0}", currentPieceOfWork.outputBuffer.Count);


					// Here is tricky: returning bytes buffer of the piece of work that was handled previously.
					// Bytes buffer of current piece (currentPieceOfWork.streamData) can still be used
					// by a thread processing the piece following the current one.
					if (currentParams.Direction == MessagesParserDirection.Forward)
						SafeReturnStreamDataToThePool(currentPieceOfWork.prevStreamData);
					else
						SafeReturnStreamDataToThePool(currentPieceOfWork.nextStreamData);

					foreach (var m in currentPieceOfWork.outputBuffer)
					{
						yield return m;
					}

					var tmp = Interlocked.Decrement(ref peicesOfWorkBeingProgressed);
					tracer.Info("Finished consuming piece of work #{0} ({1} are still being processed)", currentPieceOfWork.id, tmp);

					ReturnOutputBufferToThePool(currentPieceOfWork.outputBuffer);
				}
			}

			tracer.Info("Enumerator exited");
		}

		void SafeReturnStreamDataToThePool(StreamData streamData)
		{
			if (!streamData.IsEmpty && streamData.Bytes.Length == BytesToParsePerThread)
			{
				tracer.Info("Returning stream data to the pool: {0:x8}", streamData.Bytes.GetHashCode());
				streamDataPool.Release(streamData.Bytes);
				tracer.Info("Stream data pool size: {0}", streamDataPool.FreeObjectsCount);
			}
		}

		void ReturnOutputBufferToThePool(List<PostprocessedMessage> buffer)
		{
			buffer.Clear();
			tracer.Info("Returning output buffer to the pool: {0:x8}", buffer.GetHashCode());
			outputBuffersPool.Release(buffer);
			tracer.Info("Output buffer pool size: {0}", outputBuffersPool.FreeObjectsCount);
		}

		Byte[] AllocateStreamData()
		{
			var ret = streamDataPool.LockAndGet();
			tracer.Info("Allocated peice of stream data {0:x8}, pool size after allocation: {1}",
				ret.GetHashCode(), streamDataPool.FreeObjectsCount);
			return ret;
		}

		List<PostprocessedMessage> AllocateOutputBuffer()
		{
			var ret = outputBuffersPool.LockAndGet();
			tracer.Info("Allocated output buffer {0:x8}, pool size after allocation: {1}",
				ret.GetHashCode(), outputBuffersPool.FreeObjectsCount);
			return ret;
		}

		readonly LJTraceSource tracer = LJTraceSource.EmptyTracer;
		readonly ThreadSafeObjectPool<Byte[]> streamDataPool;
		readonly ThreadSafeObjectPool<List<PostprocessedMessage>> outputBuffersPool;
		readonly MessagesSplitterFlags splitterFlags;
		readonly bool useMockThreading;

		ISequentialMediaReaderAndProcessorMock mockedReaderAndProcessor;
		IEnumerator<PostprocessedMessage> enumer;
		CreateParserParams currentParams;
		int nextPieceOfWorkId;
		int lastThreadLocalStateId;
		int peicesOfWorkBeingProgressed;
		bool attachedToParser;

		#endregion
	}
}
