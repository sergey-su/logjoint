using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.Telemetry
{
    public class WinFormsUnhandledExceptionsReporter
    {
        public static void Setup(ITelemetryCollector telemetryCollector)
        {
            Application.ThreadException += (sender, e) =>
            {
                telemetryCollector.ReportException(e.Exception, "Application.ThreadException");
            };
        }
    }
}
