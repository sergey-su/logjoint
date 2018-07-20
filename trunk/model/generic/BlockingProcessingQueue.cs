using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public class BlockingProcessingQueue<T>: IDisposable
	{
		public interface IUnderlyingCollection: IDisposable
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
			(underlyingCollection as IDisposable)?.Dispose();
		}

		internal void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("BlockingProcessingQueue");
		}

		private IUnderlyingCollection underlyingCollection;
		private bool disposed;
	}
}
