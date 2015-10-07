using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.Telemetry
{
	public class WinFormsUnhandledExceptionsReporter: UnhandledExceptionsReporter
	{
		public WinFormsUnhandledExceptionsReporter(ITelemetryCollector telemetryCollector): base(telemetryCollector)
		{
			Application.ThreadException += (sender, e) =>
			{
				telemetryCollector.ReportException(e.Exception, "Application.ThreadException");
			};
		}
	}
}
