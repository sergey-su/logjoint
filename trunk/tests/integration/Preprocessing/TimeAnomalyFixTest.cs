using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.StatusReports;
using NFluent;

namespace LogJoint.Tests.Integration
{
    [IntegrationTestFixture]
    internal class TimeAnomalyFixTest
    {
        [IntegrationTest]
        public static async Task DetectsAndFixesTimeAnomaly(TestAppInstance app)
        {
            var testFolder = app.ModelObjects.TempFilesManager.GenerateNewName();
            Directory.CreateDirectory(testFolder);
            var testLog = Path.Combine(testFolder, "upside-down.log");

            IEnumerable<int> generateSeconds()
            {
                yield return 1;
                for (int second = 19; second > 1; second--)
                {
                    yield return second;
                }
                yield return 20;
            };

            using (var logWriter = new StreamWriter(testLog))
            {
                foreach (int second in generateSeconds())
                {
                    logWriter.WriteLine($@"SampleApp Start: 0 : test{second:d2}
  ProcessId=69745
  ThreadId=1
  DateTime=2021-04-25T01:01:{second:d2}.0000000Z");
                }
            }

            await app.OpenFileAs(testLog, "Microsoft", "TextWriterTraceListener");

            await app.WaitFor(() =>
            {
                return app.PresentationObjects.ViewModels.StatusReports.PopupData != null;
            });

            Check.That(app.PresentationObjects.ViewModels.StatusReports.PopupData.Parts.First().Text)
                .Contains("has problem with timestamps");
            Check.That(app.PresentationObjects.ViewModels.StatusReports.PopupData.Parts.ElementAt(3).Text)
                .Contains("reorder log");

            ((MessageLink)app.PresentationObjects.ViewModels.StatusReports.PopupData.Parts.ElementAt(3)).Click();

            await app.WaitFor(() =>
            {
                return app.PresentationObjects.ViewModels.StatusReports.PopupData == null;
            });

            await app.WaitForLogDisplayed(
                "test01\ntest02\ntest03\ntest04\ntest05\ntest06\ntest07\ntest08\ntest09\ntest10\ntest11\ntest12\ntest13\ntest14\ntest15\ntest16\ntest17\ntest18\ntest19\ntest20");
        }
    }
}
