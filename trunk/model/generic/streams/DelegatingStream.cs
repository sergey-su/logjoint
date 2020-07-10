using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public class DelegatingStream: Stream
	{
		Stream impl;
		bool ownImpl;
		bool disposed;

		void CheckImpl()
		{
			if (impl == null)
				throw new InvalidOperationException("Operation cannot be performed when there is no stream to delegate");
		}

		void Reset()
		{
			if (impl != null && ownImpl)
			{
				impl.Dispose();
			}
			impl = null;
			ownImpl = false;
		}

		public DelegatingStream(Stream stream = null, bool ownStream = false)
		{
			SetStream(stream, ownStream);
		}

		protected override void Dispose(bool disposing)
		{
			disposed = true;
			if (disposing)
			{
				Reset();
			}
			base.Dispose(disposing);
		}

		public void SetStream(Stream value, bool ownStream)
		{
			Reset();
			impl = value;
			ownImpl = ownStream;
		}

		public bool IsDisposed
		{
			get { return disposed; }
		}

		public override bool CanRead
		{
			get 
			{
				CheckImpl();
				return impl.CanRead;
			}
		}

		public override bool CanSeek
		{
			get 
			{
				CheckImpl();
				return impl.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get 
			{
				CheckImpl();
				return impl.CanWrite;
			}
		}

		public override void Flush()
		{
			CheckImpl();
			impl.Flush();
		}

		public override long Length
		{
			get 
			{
				CheckImpl();
				return impl.Length;
			}
		}

		public override long Position
		{
			get
			{
				CheckImpl();
				return impl.Position;
			}
			set
			{
				CheckImpl();
				impl.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckImpl();
			return impl.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
			CheckImpl();
			return impl.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckImpl();
			return impl.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			CheckImpl();
			impl.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckImpl();
			impl.Write(buffer, offset, count);
		}
	}
}
