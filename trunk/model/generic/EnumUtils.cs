using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace LogJoint
{
	public static class EnumUtils
	{
		public static T NThElement<T>(IEnumerable<T> coll, int index)
		{
			int i = 0;
			foreach (T val in coll)
			{
				if (i == index)
					return val;
				++i;
			}
			throw new ArgumentOutOfRangeException("index", "There is no item with index " + index.ToString());
		}

		public static IEnumerable<KeyValuePair<int, T>> ZipWithIndex<T>(this IEnumerable<T> coll)
		{
			int idx = 0;
			foreach (var i in coll)
				yield return new KeyValuePair<int, T>(idx++, i);
		}

		public static T FirstOrDefault<T>(this IEnumerable<T> coll, T defaultValue)
		{
			foreach (var i in coll)
				return i;
			return defaultValue;
		}

		public static T FirstOrDefault<T>(this IEnumerable<T> coll, Predicate<T> predecate, T defaultValue)
		{
			foreach (var i in coll)
				if (predecate(i))
					return i;
			return defaultValue;
		}

		public static int? IndexOf<T>(this IEnumerable<T> coll, Func<T, bool> predecate)
		{
			int currentIdx = 0;
			foreach (var item in coll)
			{
				if (predecate(item))
					return currentIdx;
				++currentIdx;
			}
			return null;
		}

		public static IEnumerable<T> Union<T>(this IEnumerable<T> coll, T val)
		{
			foreach (var i in coll)
				yield return i;
			yield return val;
		}

		public static T Min<T>(this IEnumerable<T> coll, Func<T, T, bool> firstArgLessThanSecondPredicate)
		{
			T ret = default(T);
			int idx = 0;
			foreach (var x in coll)
			{
				if (idx == 0)
					ret = x;
				else if (firstArgLessThanSecondPredicate(x, ret))
					ret = x;
				++idx;
			}
			return ret;
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> coll1, IEnumerable<T> coll2)
		{
			return SymmetricDifference(coll1, coll2, Comparer<T>.Default);
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> coll1, IEnumerable<T> coll2, IComparer<T> cmp)
		{
		    using (IEnumerator<T> enum1 = coll1.GetEnumerator())
			using (IEnumerator<T> enum2 = coll2.GetEnumerator())
			{
				bool enum1valid = enum1.MoveNext();
				bool enum2valid = enum2.MoveNext();
				while (enum1valid && enum2valid)
				{
					int cmpResult = cmp.Compare(enum1.Current, enum2.Current);
					if (cmpResult < 0)
					{
						yield return enum1.Current;
						enum1valid = enum1.MoveNext();
					}
					else if (cmpResult > 0)
					{
						yield return enum2.Current;
						enum2valid = enum2.MoveNext();
					}
					else
					{
						enum1valid = enum1.MoveNext();
						enum2valid = enum2.MoveNext();
					}
				}
				while (enum1valid)
				{
					yield return enum1.Current;
					enum1valid = enum1.MoveNext();
				}
				while (enum2valid)
				{
					yield return enum2.Current;
					enum2valid = enum2.MoveNext();
				}
			}
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> coll)
		{
			return new HashSet<T>(coll);
		}

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
						finally 
						{ 
							if (Interlocked.Decrement(ref _remainingPartitions) == 0) 
								_sharedEnumerator.Dispose(); 
						}
					}
				}
			}
		}

		public static V Upsert<K, V>(this Dictionary<K, V> dict, K key, Func<V> newValueFactory, Action<V> updater) where V: class
		{
			V value;
			if (dict.TryGetValue(key, out value))
			{
				updater(value);
			}
			else
			{
				dict.Add(key, newValueFactory());
			}
			return value;
		}

		public static IEnumerable<T> MergeSortedSequences<T>(this IEnumerable<T>[] enums, IComparer<T> valueComparer)
		{
			var comparer = new EnumeratorsComparer<T>(valueComparer);
			var iters = new VCSKicksCollection.PriorityQueue<IEnumerator<T>>(comparer);
			try
			{
				foreach (var e in enums)
				{
					var i = e.GetEnumerator();
					if (i.MoveNext())
						iters.Enqueue(i);
				}
				for (; iters.Count > 0; )
				{
					var i = iters.Dequeue();
					try
					{
						yield return i.Current;
						if (i.MoveNext())
						{
							iters.Enqueue(i);
							i = null;
						}
					}
					finally
					{
						if (i != null)
							i.Dispose();
					}
				}
			}
			finally
			{
				while (iters.Count != 0)
					iters.Dequeue().Dispose();
			}
		}

		class EnumeratorsComparer<T> : IComparer<IEnumerator<T>>
		{
			readonly IComparer<T> valueComparer;

			public EnumeratorsComparer(IComparer<T> valueComparer)
			{
				this.valueComparer = valueComparer;
			}

			int IComparer<IEnumerator<T>>.Compare(IEnumerator<T> x, IEnumerator<T> y)
			{
				return valueComparer.Compare(x.Current, y.Current);
			}
		};
	}
}

