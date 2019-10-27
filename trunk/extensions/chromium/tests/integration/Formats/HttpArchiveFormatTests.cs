using System.Threading.Tasks;
using System.IO;
using NFluent;

namespace LogJoint.Tests.Integration.Chromium
{
	[IntegrationTestFixture]
	public class HttpArchiveFormatTests
	{
		[IntegrationTest]
		async Task LoadsLogFileAndEnablesPostprocessors(IContext context)
		{
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("www.hemnet.se.har"));

			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);

			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[3].Value).IsEqualTo("entry#147 I header  date: Sat, 13 Jul 2019 13:36:59 GMT");

			Check.That(context.Presentation.Postprocessing.SummaryView.Timeline.Enabled).IsTrue();
			Check.That(context.Presentation.Postprocessing.SummaryView.SequenceDiagram.Enabled).IsTrue();
		}

		[IntegrationTest]
		public async Task CanLoadBrokenHarFile(IContext context)
		{
			var tempHarFileName = Path.Combine(context.AppDataDirectory, "broken.har");
			var harContent = File.ReadAllText(await context.Samples.GetSampleAsLocalFile("www.hemnet.se.har"));
			harContent = harContent.Substring(0, harContent.Length - 761); // break HAR json by randomly cutting the tail
			File.WriteAllText(tempHarFileName, harContent);
			await context.Utils.EmulateFileDragAndDrop(tempHarFileName);

			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);

			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[1].Value).IsEqualTo(
				"entry#147 I receive http/2.0+quic/46 200");
		}
	}
}
