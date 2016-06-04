using System;

namespace LogJoint.Profiling
{
	public class Operation: IDisposable
	{
		readonly LJTraceSource trace;
		readonly string name;
		bool disposed;

		public static readonly Operation Null = new Operation();

		public Operation(LJTraceSource trace, string name)
		{
			this.trace = trace;
			this.name = name;
			LogPoint("started", null);
		}

		private Operation()
		{
			disposed = true;
		}

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			LogPoint("stopped", null);
		}

		public void Milestone(string data)
		{
			if (disposed)
				return;
			LogPoint("milestone", data);
		}

		void LogPoint(string pointType, string data)
		{
			trace.Info(data == null ? "perfop '{0}' {1}" : "perfop '{0}' {1} '{2}'", name, pointType, data);
		}
	};
}
