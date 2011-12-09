#define SequentialMediaReaderAndProcessor_PLinqImplementation

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace LogJoint
{
	public interface ISequentialMediaReaderAndProcessor<ProcessedData> : IDisposable
	{
		ProcessedData ReadAndProcessNextPieceOfData();
	}

	/// <summary>
	/// Implements parallel processing of data being read from a sequential media. 
	/// An example of a sequential media is a stream opened for sequential reading.
	/// SequentialMediaReaderAndProcessor is applicable for the scenarios when
	/// only one thread is allowed to read raw data from the media at a time
	/// but many threads can process pieces of raw data concurrently.
	/// SequentialMediaReaderAndProcessor reads data upfront and processes it concurrently.
	/// SequentialMediaReaderAndProcessor itself is not thread safe i.e. must be created 
	/// and called from one client thread.
	/// </summary>
	public class SequentialMediaReaderAndProcessor<RawData, ProcessedData, ThreadLocalState> :
			ISequentialMediaReaderAndProcessor<ProcessedData>,
			IDisposable
		where ProcessedData : class
		where RawData : class
	{
		public interface ICallback
		{
			/// <summary>
			/// Reads the next piece of data from underlying media.
			/// Guaranteed to be called from one thread at a time.
			/// </summary>
			/// <returns>Opaque objects that store raw data. Will be passed to ProcessRawData() later</returns>
			IEnumerable<RawData> ReadRawDataFromMedia(CancellationToken cancellationToken);
			/// <summary>
			/// SequentialMediaReaderAndProcessor can process data in several threads.
			/// Each thread can have its own thread local data to avoid contention.
			/// This function will be called for each thread spawned to process data.
			/// </summary>
			ThreadLocalState InitializeThreadLocalState();
			/// <summary>
			/// Called when thread local data is not needed anymore
			/// </summary>
			void FinalizeThreadLocalState(ref ThreadLocalState state);
			/// <summary>
			/// Converts raw data to processed data.
			/// Can be called concurrently. Guaranteed to be called once for 
			/// a particular rawData object.
			/// </summary>
			/// <param name="rawData">Raw data object prevoisly returned by ReadRawDataFromMedia.</param>
			/// <returns>Opaque object that stores processed data. Passed rawData may be returned.
			/// This object will be eventually returned by ReadAndProcessNextPeiceOfData()</returns>
			ProcessedData ProcessRawData(RawData rawData, ThreadLocalState threadLocalState, CancellationToken cancellationToken);
		};

#if SequentialMediaReaderAndProcessor_PLinqImplementation
		#region Public interface

		public SequentialMediaReaderAndProcessor(ICallback callback, int processingQueueSize = 64)
		{
			this.callback = callback;
			this.cancellationTokenSource = new CancellationTokenSource();
			this.threadLocalStates = new List<ThreadLocalHolder>();
			this.threadLocal = new ThreadLocal<ThreadLocalHolder>(() => 
			{
				var holder = new ThreadLocalHolder() { State = callback.InitializeThreadLocalState() };
				lock (this.threadLocalStates)
					this.threadLocalStates.Add(holder);
				return holder;
			});
			this.processingQueueSize = processingQueueSize;
			this.inEnumerator = callback.ReadRawDataFromMedia(cancellationTokenSource.Token).GetEnumerator();
			this.outEnumerator = CreateEnumerator().GetEnumerator();
		}

		/// <summary>
		/// Return current piece of processed data and advances the reader.
		/// </summary>
		/// <returns>Object that has been returned by ICallback.ProcessRawData.
		/// null indicates end of sequence.</returns>
		public ProcessedData ReadAndProcessNextPieceOfData()
		{
			CheckDisposed();
			if (outEnumerator.MoveNext())
				return outEnumerator.Current;
			return null;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			inEnumerator.Dispose();
			cancellationTokenSource.Cancel();
			outEnumerator.Dispose();
			threadLocal.Dispose();
			foreach (var state in threadLocalStates)
				callback.FinalizeThreadLocalState(ref state.State);
		}

		#endregion

		#region Implementation

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("SequentialMediaReaderAndProcessor");
		}

		IEnumerable<RawDataHolder> FetchSourceItems()
		{
			for (; ; )
			{
				var holder = new RawDataHolder();
				if (Interlocked.Increment(ref itemsBeingProcessed) > processingQueueSize)
				{
					Interlocked.Decrement(ref itemsBeingProcessed);
					yield break;
				}
				if (inEnumerator.MoveNext())
				{
					holder.Data = inEnumerator.Current;
					yield return holder;
				}
				else
				{
					yield return null;
					yield break;
				}
			}
		}

		IEnumerable<ProcessedData> CreateEnumerator()
		{
			var cancellationToken = cancellationTokenSource.Token;
			while (true)
			{
				foreach (var processedData in FetchSourceItems().AsParallel().AsOrdered().WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(rawDataHolder =>
					rawDataHolder != null ? callback.ProcessRawData(rawDataHolder.Data, threadLocal.Value.State, cancellationToken) : null
				))
				{
					Interlocked.Decrement(ref itemsBeingProcessed);
					if (processedData == null)
						yield break;
					yield return processedData;
				}
				++timesConveyorRestarted;
			}
		}

		class ThreadLocalHolder
		{
			public ThreadLocalState State;
		};

		class RawDataHolder
		{
			public RawData Data;
		}

		readonly ICallback callback;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly ThreadLocal<ThreadLocalHolder> threadLocal;
		readonly List<ThreadLocalHolder> threadLocalStates;
		readonly int processingQueueSize;
		readonly IEnumerator<RawData> inEnumerator;
		readonly IEnumerator<ProcessedData> outEnumerator;

		int itemsBeingProcessed;
		long timesConveyorRestarted;
		bool disposed;

		#endregion

#else // SequentialMediaReaderAndProcessor_PLinqImplementation
		
		#region Public interface

		public SequentialMediaReaderAndProcessor(ICallback callback)
		{
			this.callback = callback;
			this.outputQueue = new BlockingProcessingQueue<OutputQueueEntry>(new UnderlyingOutputQueue());
			this.cancellationTokenSource = new CancellationTokenSource();
			this.processingTask = new Task(() =>
			{
				ParallelOptions opts = new ParallelOptions();
				//opts.MaxDegreeOfParallelism = 1; // uncomment that to simplify debugging
				Parallel.ForEach(
					MakeOutputQueue(),
					opts,
					callback.InitializeThreadLocalState,
					(token, state, tls) =>
					{
						var entry = token.Value;
						entry.Output = entry.Input != null ? callback.ProcessRawData(entry.Input, tls, opts.CancellationToken) : null;
						token.Value = entry;
						token.MarkAsProcessed();
						return tls;
					},
					(tls) => { }
				);
			});
			this.processingTask.Start();
		}

		/// <summary>
		/// Return current piece of processed data and advances the reader.
		/// </summary>
		/// <returns>Object that has been returned by ICallback.ProcessRawData.
		/// null indicates end of sequence.</returns>
		public ProcessedData ReadAndProcessNextPieceOfData()
		{
			CheckDisposed();
			return outputQueue.Take().Output;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			cancellationTokenSource.Cancel();
			processingTask.Wait();
			outputQueue.Dispose();
			cancellationTokenSource.Dispose();
		}

	#endregion

	#region Implementation

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("SequentialMediaReaderAndProcessor");
		}

		struct OutputQueueEntry
		{
			public RawData Input;
			public ProcessedData Output;
		};

		class UnderlyingOutputQueue : BlockingCollection<BlockingProcessingQueue<OutputQueueEntry>.Token>, BlockingProcessingQueue<OutputQueueEntry>.IUnderlyingCollection
		{
			public UnderlyingOutputQueue() :
				base(new ConcurrentQueue<BlockingProcessingQueue<OutputQueueEntry>.Token>(), 16)
			{
			}
		};

		IEnumerable<BlockingProcessingQueue<OutputQueueEntry>.Token> MakeOutputQueue()
		{
			foreach (var rawData in callback.ReadRawDataFromMedia(cancellationTokenSource.Token))
			{
				if (cancellationTokenSource.IsCancellationRequested)
					break;
				OutputQueueEntry entry;
				entry.Input = rawData;
				entry.Output = null;
				yield return outputQueue.Add(entry);
			}
			yield return outputQueue.Add(new OutputQueueEntry());
		}

		readonly ICallback callback;
		readonly BlockingProcessingQueue<OutputQueueEntry> outputQueue;
		readonly Task processingTask;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly Diagnostics.AverageLong aveOutputQueueLength = new Diagnostics.AverageLong();

		bool disposed;

	#endregion

#endif
	};

	public class Temp
	{
		static public IEnumerable<ProcessedData> BoundedParallelSelect<RawData, ProcessedData, ThreadLocalState>(IEnumerable<RawData> source,
			Func<ThreadLocalState> threadLocalInit, Action<ThreadLocalState> threadLocalFinialize, int queueSize)
		{
			return null;
		}
	}

	internal interface ISequentialMediaReaderAndProcessorMock
	{
		void SpawnWorkerThread(int threadIdx);
		void FinishWorkerThread(int threadIdx);
		void ReadNextPieceOfRawData();
		void ProcessNextPieceOfRawData(int threadIdx, int rawDataIdx);
	}

	/// <summary>
	/// Mock implementation of ISequentialMediaReaderAndProcessor that one can use to replace real SequentialMediaReaderAndProcessor 
	/// in unit tests.
	/// </summary>
	internal class SequentialMediaReaderAndProcessorMock<RawData, ProcessedData, ThreadLocalState> : 
			ISequentialMediaReaderAndProcessor<ProcessedData>, IDisposable,	ISequentialMediaReaderAndProcessorMock
		where ProcessedData : class
		where RawData : class
	{
		public SequentialMediaReaderAndProcessorMock(SequentialMediaReaderAndProcessor<RawData, ProcessedData, ThreadLocalState>.ICallback callback)
		{
			this.callback = callback;
		}

		#region ISequentialMediaReaderAndProcessor interface implementation

		public ProcessedData ReadAndProcessNextPieceOfData()
		{
			var entry = processingQueue.Dequeue();
			if (!entry.Processed)
				throw new InvalidOperationException("Next piece of data is not ready");
			return entry.Output;
		}

		public void Dispose()
		{
			if (enumer == null)
			{
				cancellationTokenSource.Cancel();
				enumer.Dispose();
				enumer = null;
			}
		}

		#endregion

		#region ISequentialMediaReaderAndProcessorMock methods to be called from unit tests

		public void SpawnWorkerThread(int threadIdx)
		{
			if (threads.ContainsKey(threadIdx))
				throw new InvalidOperationException("Thread with given index already exists");
			threads[threadIdx] = callback.InitializeThreadLocalState();
		}

		public void FinishWorkerThread(int threadIdx)
		{
			if (!threads.ContainsKey(threadIdx))
				throw new ArgumentException("Thread doesn't exist", "threadIdx");
			threads.Remove(threadIdx);
		}

		public void ReadNextPieceOfRawData()
		{
			if (enumer == null)
				enumer = callback.ReadRawDataFromMedia(cancellationTokenSource.Token).GetEnumerator();
			processingQueue.Enqueue(new Entry() { Input = enumer.MoveNext() ? enumer.Current : null });
		}

		public void ProcessNextPieceOfRawData(int threadIdx, int rawDataIdx)
		{
			if (!threads.ContainsKey(threadIdx))
				throw new ArgumentException("Thread doesn't exist", "threadIdx");
			if (rawDataIdx >= processingQueue.Count)
				throw new ArithmeticException("rawDataIdx");
			var entry = processingQueue.ToArray()[rawDataIdx];
			if (entry.Processed)
				throw new InvalidOperationException("This piece of raw data is already processed");
			entry.Processed = true;
			if (entry.Input == null)
				entry.Output = null;
			else
				entry.Output = callback.ProcessRawData(entry.Input, threads[threadIdx], cancellationTokenSource.Token);
		}

		#endregion

		#region Implementation

		class Entry
		{
			public RawData Input;
			public ProcessedData Output;
			public bool Processed;
		};

		readonly SequentialMediaReaderAndProcessor<RawData, ProcessedData, ThreadLocalState>.ICallback callback;
		readonly Dictionary<int, ThreadLocalState> threads = new Dictionary<int, ThreadLocalState>();
		readonly Queue<Entry> processingQueue = new Queue<Entry>();
		readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		IEnumerator<RawData> enumer;

		#endregion
	}
}