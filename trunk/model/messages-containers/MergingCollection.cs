using System;
using System.Collections.Generic;

namespace LogJoint.MessagesContainers
{
	public abstract class MergingCollection : IMessagesCollection
	{
		protected abstract IEnumerable<IMessagesCollection> GetCollectionsToMerge();
		protected virtual void Lock()
		{
		}
		protected virtual void Unlock()
		{
		}

		public class MessagesComparer : IComparer<IMessage>
		{
			bool reverse;
			bool singleCollectionMode;

			public MessagesComparer(bool reverse, bool singleCollectionMode = true)
			{
				this.reverse = reverse;
				this.singleCollectionMode = singleCollectionMode;
			}

			public void ResetSingleCollectionMode()
			{
				singleCollectionMode = false;
			}

			static public int Compare(IMessage m1, IMessage m2, bool skipConnectionIdComparision)
			{
				int sign = MessageTimestamp.Compare(m1.Time, m2.Time);
				if (sign == 0)
				{
					if (!skipConnectionIdComparision)
					{
						var connectionId1 = m1.Thread.LogSource.ConnectionId;
						var connectionId2 = m2.Thread.LogSource.ConnectionId;
						sign = string.CompareOrdinal(connectionId1, connectionId2);
					}
					if (sign == 0)
					{
						sign = Math.Sign(m1.Position - m2.Position);
					}
					if (sign == 0)
					{
						sign = Math.Sign(m1.GetHashCode() - m2.GetHashCode());
					}
				}
				return sign;
			}

			public int Compare(IMessage m1, IMessage m2)
			{
				int ret = Compare(m1, m2, singleCollectionMode);
				return reverse ? -ret : ret;
			}
		};

		#region ILinesCollection Members

		public int Count
		{
			get
			{
				Lock();
				try
				{
					int ret = 0;
					foreach (IMessagesCollection c in GetCollectionsToMerge())
						ret += c.Count;
					return ret;
				}
				finally
				{
					Unlock();
				}
			}
		}

		class EnumeratorsComparer : MessagesComparer, IComparer<IEnumerator<IndexedMessage>>
		{
			public EnumeratorsComparer(bool reverse) : base(reverse) { }

			public int Compare(IEnumerator<IndexedMessage> x, IEnumerator<IndexedMessage> y)
			{
				return base.Compare(x.Current.Message, y.Current.Message);
			}
		};

		public IEnumerable<IndexedMessage> Forward(int startPos, int endPosition)
		{
			Lock();
			try
			{
				var comparer = new EnumeratorsComparer(false);
				int totalCount = 0;
				VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>> iters = new VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>>(comparer);
				try
				{
					int collectionsCount = 0;
					foreach (IMessagesCollection l in GetCollectionsToMerge())
					{
						++collectionsCount;
						if (collectionsCount > 1)
							comparer.ResetSingleCollectionMode();
						int localCount = l.Count;
						totalCount += localCount;
						IEnumerator<IndexedMessage> i = l.Forward(0, localCount).GetEnumerator();
						if (i.MoveNext())
							iters.Enqueue(i);
					}
					startPos = RangeUtils.PutInRange(0, totalCount, startPos);
					endPosition = RangeUtils.PutInRange(0, totalCount, endPosition);

					if (collectionsCount == 1) // optimized version for the case when there is only one collection to merge
					{
						using (IEnumerator<IndexedMessage> i = iters.Dequeue())
						{
							for (int idx = 0; idx < endPosition; ++idx)
							{
								if (idx >= startPos)
									yield return new IndexedMessage(idx, i.Current.Message);
								if (!i.MoveNext())
									break;
							}
						}
					}
					else
					{
						for (int idx = 0; idx < endPosition; ++idx)
						{
							IEnumerator<IndexedMessage> i = iters.Dequeue();
							try
							{
								if (idx >= startPos)
									yield return new IndexedMessage(idx, i.Current.Message);
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
				}
				finally
				{
					while (iters.Count != 0)
						iters.Dequeue().Dispose();
				}
			}
			finally
			{
				Unlock();
			}
		}

		public IEnumerable<IndexedMessage> Reverse(int startPos, int endPosition)
		{
			Lock();
			try
			{
				var comparer = new EnumeratorsComparer(true);
				VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>> iters = new VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>>(comparer);
				try
				{
					int c = 0;
					foreach (IMessagesCollection l in GetCollectionsToMerge())
					{
						int lc = l.Count;
						c += lc;
						IEnumerator<IndexedMessage> i = l.Reverse(lc - 1, -1).GetEnumerator();
						if (i.MoveNext())
							iters.Enqueue(i);
					}
					startPos = RangeUtils.PutInRange(-1, c - 1, startPos);
					endPosition = RangeUtils.PutInRange(-1, c - 1, endPosition);
					for (int idx = c - 1; idx > endPosition; --idx)
					{
						IEnumerator<IndexedMessage> i = iters.Dequeue();
						try
						{
							if (idx <= startPos)
								yield return new IndexedMessage(idx, i.Current.Message);
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
			finally
			{
				Unlock();
			}
		}

		#endregion
	}

}
