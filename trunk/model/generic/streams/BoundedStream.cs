using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	public class BoundedStream: DelegatingStream
	{
		public void SetBounds(long? begin, long? end)
		{
			this.end = end;
		}

		public override long Length
		{
			get
			{
				return Math.Min(base.Length, end.GetValueOrDefault(long.MaxValue));
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return base.Read(buffer, offset, Math.Min(count, (int)Math.Max(0, Length - Position)));
		}

		public override long Seek(long offset, System.IO.SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			base.SetLength(Math.Min(value, end.GetValueOrDefault(long.MaxValue)));
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		//long? begin;
		long? end;
	}
}
