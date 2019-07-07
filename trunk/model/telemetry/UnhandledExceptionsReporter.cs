using System;
using System.Threading.Tasks;

namespace LogJoint.Telemetry
{
	static class UnhandledExceptionsReporter
	{
		public static void SetupLogging(LJTraceSource tracer, IShutdown shutdown)
		{
			void unhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
			{
				string logMsg = "Unhandled domain exception occurred";
				if (e.ExceptionObject is Exception ex)
					tracer.Error((Exception)e.ExceptionObject, logMsg);
				else
					tracer.Error("{0}: ({1}) {2}", logMsg, e.ExceptionObject.GetType(), e.ExceptionObject);
			}
			AppDomain.CurrentDomain.UnhandledException += unhandledExceptionEventHandler;

			void unobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs e)
			{
				tracer.Error(e.Exception, "UnobservedTaskException");
			}
			TaskScheduler.UnobservedTaskException += unobservedTaskExceptionEventHandler;

			shutdown.Phase2Cleanup += (s, e) =>
			{
				AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionEventHandler;
				TaskScheduler.UnobservedTaskException -= unobservedTaskExceptionEventHandler;
			};
		}

		public static void Setup(ITelemetryCollector telemetryCollector, IShutdown shutdown)
		{
			void unhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
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
			}

			AppDomain.CurrentDomain.UnhandledException += unhandledExceptionEventHandler;

			void unobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs e)
			{
				telemetryCollector.ReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
			}
			TaskScheduler.UnobservedTaskException += unobservedTaskExceptionEventHandler;

			shutdown.Phase2Cleanup += (s, e) =>
			{
				AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionEventHandler;
				TaskScheduler.UnobservedTaskException -= unobservedTaskExceptionEventHandler;
			};
		}
	}
}
