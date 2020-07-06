using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
	public class StringStreamMedia : ILogMedia
	{
		System.IO.MemoryStream stm = new System.IO.MemoryStream();

		public StringStreamMedia(string str, Encoding encoding)
		{
			SetData(str, encoding);
		}

		public StringStreamMedia()
		{
		}

		public void SetData(string str, Encoding encoding)
		{
			byte[] bytes = encoding.GetBytes(str);
			stm.SetLength(bytes.Length);
			bytes.CopyTo(stm.GetBuffer(), 0);
		}

		public void SetData(System.IO.Stream ms)
		{
			stm.SetLength(0);
			ms.CopyTo(stm);
		}

		public void SetData(string str)
		{
			SetData(str, EncodingUtils.GetDefaultEncoding());
		}

		#region ILogMedia Members

		public Task Update()
		{
			return Task.CompletedTask;
		}

		public bool IsAvailable
		{
			get { return true; }
		}

		public System.IO.Stream DataStream
		{
			get { return stm; }
		}

		public DateTime LastModified
		{
			get { return new DateTime(); }
		}

		public long Size
		{
			get { return stm.Length; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion
	};
}
