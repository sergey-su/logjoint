using System;
using System.Threading.Tasks;

namespace LogJoint.Telemetry
{
	static class UnhandledExceptionsReporter
	{
		public static void SetupLogging(LJTraceSource tracer)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				string logMsg = "Unhandled domain exception occurred";
				if (e.ExceptionObject is Exception ex)
					tracer.Error((Exception)e.ExceptionObject, logMsg);
				else
					tracer.Error("{0}: ({1}) {2}", logMsg, e.ExceptionObject.GetType(), e.ExceptionObject);
			};
			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				tracer.Error(e.Exception, "UnobservedTaskException");
			};
		}

		public static void Setup(ITelemetryCollector telemetryCollector)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				if (e.ExceptionObject is Exception ex)
				{
					telemetryCollector.ReportException(ex, "Domain.UnhandledException");
				}
				else
				{
					telemetryCollector.ReportException(
						new Exception(e.ExceptionObject != null ? e.ExceptionObject.GetType().ToString() : "null exception object"),
						"Domain.UnhandledException (custom exception)");
				}
			};
			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				telemetryCollector.ReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
			};
		}
	}
}
