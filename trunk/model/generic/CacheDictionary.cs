using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint
{
<<<<<<< HEAD
	public class BlockingProcessingQueue<T>: IDisposable
	{
		public interface IUnderlyingCollection
		{
			void Add(Token item);
			Token Take();
			int Count { get; }
		};

		public class Token
		{
			public void MarkAsProcessed()
			{
				owner.CheckDisposed();
				processed.Set();
			}

			public T Value
			{
				get { owner.CheckDisposed(); return value; }
				set { owner.CheckDisposed(); this.value = value; }
			}

			internal Token(BlockingProcessingQueue<T> owner, T value)
			{
				this.owner = owner;
				this.value = value;
			}

			internal void WaitUntilProcessed()
			{
				processed.Wait();
			}

			internal void Dispose()
			{
				processed.Dispose();
			}

			private readonly BlockingProcessingQueue<T> owner;
			private T value;
			private ManualResetEventSlim processed = new ManualResetEventSlim();
		};


		public BlockingProcessingQueue(IUnderlyingCollection underlyingCollection)
		{
			this.underlyingCollection = underlyingCollection;
		}

		public Token Add(T item)
		{
			CheckDisposed();
			Token ret = new Token(this, item);
			underlyingCollection.Add(ret);
			return ret;
		}

		public T Take()
		{
			CheckDisposed();
			Token token = underlyingCollection.Take();
			token.WaitUntilProcessed();
			token.Dispose();
			return token.Value;
		}

		public int Count
		{
			get 
			{
				CheckDisposed();
				return underlyingCollection.Count;			
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			while (underlyingCollection.Count > 0)
				underlyingCollection.Take().Dispose();
		}

		internal void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("BlockingProcessingQueue");
		}

		private IUnderlyingCollection underlyingCollection;
		private bool disposed;
	}
=======
	public class CacheDictionary<K, V>
	{
		public class Entry
		{
			public bool valid;
			public V value;
		};

		readonly Dictionary<K, Entry> cache = new Dictionary<K, Entry>();

		public void MarkAllInvalid()
		{
			foreach (var x in cache)
				x.Value.valid = false;
		}

		public V Get(K key, Func<K, V> factory)
		{
			Entry entry;
			if (!cache.TryGetValue(key, out entry))
				cache.Add(key, entry = new Entry() { value = factory(key) });
			entry.valid = true;
			return entry.value;
		}

		public V Get(K key)
		{
			Entry entry;
			if (!cache.TryGetValue(key, out entry))
				return default(V);
			return entry.value;
		}

		public void Cleanup()
		{
			foreach (var k in cache.Where(x => !x.Value.valid).Select(x => x.Key).ToArray())
				cache.Remove(k);
		}
	};
>>>>>>> timeline_collapse
}
