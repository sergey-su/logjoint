using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace System.Collections.Concurrent.Partitioners
{
	/// <summary>Partitions a data source one item at a time.</summary>
	public static class SingleItemPartitioner
	{
		/// <summary>Creates a partitioner for an enumerable that partitions it one item at a time.</summary>
		/// <typeparam name="T">Specifies the type of data contained in the enumerable.</typeparam>
		/// <param name="source">The source enumerable to be partitioned.</param>
		/// <returns>The partitioner.</returns>
		public static OrderablePartitioner<T> Create<T>(IEnumerable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");
			else return new SingleItemEnumerablePartitioner<T>(source);
		}

		/// <summary>Partitions an enumerable one item at a time.</summary>
		/// <typeparam name="T">Specifies the type of data contained in the list.</typeparam>
		private sealed class SingleItemEnumerablePartitioner<T> : OrderablePartitioner<T>
		{
			/// <summary>The enumerable to be partitioned.</summary>
			private readonly IEnumerable<T> _source;

			/// <summary>Initializes the partitioner.</summary>
			/// <param name="source">The enumerable to be partitioned.</param>
			internal SingleItemEnumerablePartitioner(IEnumerable<T> source) : base(true, false, true) { _source = source; }

			/// <summary>Gets whether this partitioner supports dynamic partitioning (it does).</summary>
			public override bool SupportsDynamicPartitions { get { return true; } }

			public override IList<IEnumerator<KeyValuePair<long, T>>> GetOrderablePartitions(int partitionCount)
			{
				if (partitionCount < 1) throw new ArgumentOutOfRangeException("partitionCount");
				var dynamicPartitioner = new DynamicGenerator(_source.GetEnumerator(), false);
				return (from i in Enumerable.Range(0, partitionCount) select dynamicPartitioner.GetEnumerator()).ToList();
			}

			/// <summary>Gets a list of the specified static number of partitions.</summary>
			/// <param name="partitionCount">The static number of partitions to create.</param>
			/// <returns>The list of created partitions ready to be iterated.</returns>
			public override IEnumerable<KeyValuePair<long, T>> GetOrderableDynamicPartitions()
			{
				return new DynamicGenerator(_source.GetEnumerator(), true);
			}

			/// <summary>Dynamically generates a partitions on a shared enumerator.</summary>
			private class DynamicGenerator : IEnumerable<KeyValuePair<long, T>>, IDisposable
			{
				/// <summary>The source enumerator shared amongst all partitions.</summary>
				private readonly IEnumerator<T> _sharedEnumerator;
				/// <summary>The next available position to be yielded.</summary>
				private long _nextAvailablePosition;
				/// <summary>The number of partitions remaining to be disposed, potentially including this dynamic generator.</summary>
				private int _remainingPartitions;
				/// <summary>Whether this dynamic partitioner has been disposed.</summary>
				private bool _disposed;

				/// <summary>Initializes the dynamic generator.</summary>
				/// <param name="sharedEnumerator">The enumerator shared by all partitions.</param>
				/// <param name="requiresDisposal">Whether this generator will be disposed.</param>
				public DynamicGenerator(IEnumerator<T> sharedEnumerator, bool requiresDisposal)
				{
					_sharedEnumerator = sharedEnumerator;
					_nextAvailablePosition = -1;
					_remainingPartitions = requiresDisposal ? 1 : 0;
				}

				/// <summary>Closes the shared enumerator if all other partitions have completed.</summary>
				void IDisposable.Dispose()
				{
					if (!_disposed && Interlocked.Decrement(ref _remainingPartitions) == 0)
					{
						_disposed = true;
						_sharedEnumerator.Dispose();
					}
				}

				/// <summary>Increments the number of partitions in use and returns a new partition.</summary>
				/// <returns>The new partition.</returns>
				public IEnumerator<KeyValuePair<long, T>> GetEnumerator()
				{
					Interlocked.Increment(ref _remainingPartitions);
					return GetEnumeratorCore();
				}
				IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

				/// <summary>Creates a partition.</summary>
				/// <returns>The new partition.</returns>
				private IEnumerator<KeyValuePair<long, T>> GetEnumeratorCore()
				{
					try
					{
						while (true)
						{
							T nextItem;
							long position;
							lock (_sharedEnumerator)
							{
								if (_sharedEnumerator.MoveNext())
								{
									position = _nextAvailablePosition++;
									nextItem = _sharedEnumerator.Current;
								}
								else yield break;
							}
							yield return new KeyValuePair<long, T>(position, nextItem);
						}
					}
					finally { if (Interlocked.Decrement(ref _remainingPartitions) == 0) _sharedEnumerator.Dispose(); }
				}
			}
		}
	}
}

namespace logjoint.model.tests
{
	[TestClass]
	public class SpikeUnitTest
	{

		int returnedCounter;

		IEnumerable<int> Enum()
		{
			while (true)
			{
				yield return Interlocked.Increment(ref returnedCounter);
			}
		}

		class MyParallel<T, OutT>: IDisposable
		{
			BlockingCollection<ItemHolder> queue;
			IEnumerator<T> sourceEnum;
			bool eof;
			
			public MyParallel(int bufferSize, IEnumerable<T> source)
			{
				queue = new BlockingCollection<ItemHolder>(new ConcurrentQueue<ItemHolder>(), bufferSize);
				sourceEnum = source.GetEnumerator();
			}

			public void Dispose()
			{
				sourceEnum.Dispose();
				eof = true;
			}

			class ItemHolder
			{
				public bool valid;
				public T item;
				public OutT processedItem;
				public ItemHolder Process(Func<T, OutT> func)
				{
					if (valid)
						processedItem = func(item);
					return this;
				}
			}

			IEnumerable<ItemHolder> FetchSourceItems()
			{
				for (; ; )
				{
					var holder = new ItemHolder();
					if (!queue.TryAdd(holder))
						yield break;
					if (sourceEnum.MoveNext())
					{
						holder.valid = true;
						holder.item = sourceEnum.Current;
						yield return holder;
					}
					else
					{
						eof = true;
						yield return holder;
						yield break;
					}
				}
			}

			public IEnumerable<OutT> DoProcessing(Func<T, OutT> func)
			{
				while (!eof)
				{
					foreach (var i in FetchSourceItems().AsParallel().AsOrdered().WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(item => item.Process(func)))
					{
						var queuedItem = queue.Take();
						Debug.Assert(object.ReferenceEquals(queuedItem, i));
						if (queuedItem.valid)
							yield return queuedItem.processedItem;
					}
					int halfCapacity = queue.BoundedCapacity / 2;
					while (queue.Count > halfCapacity)
					{
						var queuedItem = queue.Take();
						if (queuedItem.valid)
							yield return queuedItem.processedItem;
					}
				}
			}
		};


		IEnumerable<OutT> MyAsParallel<T, OutT>(IEnumerable<T> source, int bufferSize, Func<T, OutT> func)
		{
			using (var impl = new MyParallel<T, OutT>(bufferSize, source))
				foreach (var i in impl.DoProcessing(func))
					yield return i;
		}

		[TestMethod]
		public void SpikeTestMethod1()
		{
			StringBuilder sb = new StringBuilder();
			var ss = System.Collections.Concurrent.Partitioners.SingleItemPartitioner.Create(Enum());
			foreach (var i in MyAsParallel(Enum(), 100, a => a))
			{
				if (i == 10)
				{
					Thread.Sleep(1000);
					sb.AppendFormat("c1:{0} ", returnedCounter);
				}
				if (i == 110)
				{
					Thread.Sleep(1000);
					sb.AppendFormat("c2:{0} ", returnedCounter);
					break;
				}
			}
			Console.WriteLine(sb.ToString());
		}
	}
}
