using System.IO;

namespace LogJoint
{
	public class TraceListener : System.Diagnostics.TextWriterTraceListener
	{
		public TraceListener(string logFileName)
			: base(logFileName)
		{
			base.Writer = new StreamWriter(logFileName, false);
		}
	}
}
