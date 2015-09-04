using System;
using System.IO;

namespace LogJoint
{
	public class TraceListener : System.Diagnostics.TraceListener // todo: share with windows
	{
		readonly Lazy<TextWriter> writer;

		public TraceListener(string logFileName)
			: base(logFileName)
		{
			writer = new Lazy<TextWriter>(() =>
			{
				try
				{
					return new StreamWriter(logFileName, false);
				}
				catch
				{
					return null;
				}
			}, true);
		}

		public override void Close()
		{
			if (!writer.IsValueCreated)
				return;
			try
			{
				writer.Value.Close();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
					this.Close();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public override void Flush()
		{
			var w = writer.Value;
			if (w == null)
				return;
			try
			{
				w.Flush();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public override void Write(string message)
		{
			var w = writer.Value;
			if (w == null)
				return;
			if (base.NeedIndent)
			{
				this.WriteIndent();
			}
			try
			{
				w.Write(message);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public override void WriteLine(string message)
		{
			var w = writer.Value;
			if (w == null)
				return;
			if (base.NeedIndent)
			{
				this.WriteIndent();
			}
			try
			{
				w.WriteLine(message);
				base.NeedIndent = true;
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}
}
