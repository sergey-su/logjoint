using System;

namespace LogJoint.Profiling
{
	public class Operation: IDisposable
	{
		readonly LJTraceSource trace;
		readonly string name;
		readonly string id;
		bool disposed;

		public static readonly Operation Null = new Operation();

		public Operation(LJTraceSource trace, string name)
		{
			this.trace = trace;
			this.name = name;
			this.id = GetHashCode().ToString("x8");
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
			trace.Info(data == null ? "perfop #{0} '{1}' {2}" : "perfop #{0} '{1}' {2} '{3}'", id, name, pointType, data);
		}
	};

	public static class TracingExtensions
	{
		public static void LogUserAction(this LJTraceSource trace, string action)
		{
			trace.Info("user action: {0}", action);
		}
	};
}
