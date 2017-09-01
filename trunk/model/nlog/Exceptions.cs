using System;

namespace LogJoint.NLog
{
	public class ImportException : Exception
	{
		public ImportException(string msg) : base(msg) { }
	};

	public class ImportErrorDetectedException : Exception
	{
		public readonly ImportLog Log;
		public ImportErrorDetectedException(string msg, ImportLog log) : base(msg) { Log = log; }
	};
}
