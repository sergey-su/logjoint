using System;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace LogJoint.Telemetry
{
	public class UnhandledExceptionsReporter
	{
		public UnhandledExceptionsReporter(ITelemetryCollector telemetryCollector)
		{
			Application.ThreadException += (sender, e) =>
			{
				telemetryCollector.ReportException(e.Exception, "Application.ThreadException");
			};
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
