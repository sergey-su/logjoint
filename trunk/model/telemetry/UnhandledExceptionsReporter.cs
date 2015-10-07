using System;
using System.Threading.Tasks;

namespace LogJoint.Telemetry
{
	public class UnhandledExceptionsReporter
	{
		public UnhandledExceptionsReporter(ITelemetryCollector telemetryCollector)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				var ex = e.ExceptionObject as Exception;
				if (ex != null)
					telemetryCollector.ReportException(ex, "Domain.UnhandledException");
				else
					telemetryCollector.ReportException(
						new Exception(e.ExceptionObject != null ? e.ExceptionObject.GetType().ToString() : "null exception object"), 
						"Domain.UnhandledException (custom exception)");
			};
			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				telemetryCollector.ReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
			};
		}
	}
}
