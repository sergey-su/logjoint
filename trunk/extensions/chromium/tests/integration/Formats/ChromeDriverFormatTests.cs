using System.Threading.Tasks;
using System.IO;
using LogJoint.Chromium.ChromeDriver;
using LogJoint.Postprocessing;
using System.Threading;
using NFluent;

namespace LogJoint.Tests.Integration.Chromium
{
	[IntegrationTestFixture]
	class ChromeDriverFormatTests
	{
		[IntegrationTest]
		public async Task LoadsLogFileAndEnablesPostprocessors(IContext context)
		{
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("chromedriver_1.log"));

			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);

			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[2].Value).IsEqualTo(
				"[1548250986.197][INFO]: Waiting for pending navigations...");

			var postprocessorsControls = context.Presentation.Postprocessing.SummaryView;
			Check.That(postprocessorsControls.Timeline.Enabled).IsTrue();
		}

		[IntegrationTest]
		public async Task ChromeDriver_SplitAndComposeTest(IContext context)
		{
			using (var testStream = await context.Samples.GetSampleAsStream("chromedriver_2019_01_23.log"))
			{
				var actualContent = new MemoryStream();

				var reader = new Reader(context.Model.Postprocessing.TextLogParser, CancellationToken.None);
				var writer = new Writer();

				await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

				Check.That(
					Helpers.SplitTextStream(actualContent)
				).ContainsExactly(
					Helpers.SplitTextStream(testStream)
				);
			}
		}

		[IntegrationTest]
		public async Task ChromeDriver_SplitAndCompose_WithForeignLogging(IContext context)
		{
			using (var testStream = await context.Samples.GetSampleAsStream("chromedriver_2019_01_22.log"))
			{
				var actualContent = new MemoryStream();

				var reader = new Reader(context.Model.Postprocessing.TextLogParser, CancellationToken.None);
				var writer = new Writer();

				await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

				Check.That(
					Helpers.SplitTextStream(actualContent)
				).ContainsExactly(
					Helpers.SplitTextStream(testStream)
				);
			}
		}

		[IntegrationTest]
		public async Task ForeignLoggingAtEndOfMssagesIsIgnored(IContext context)
		{
			using (var testStream = await context.Samples.GetSampleAsStream("chromedriver_2019_01_22.log"))
			{
				var messages = await (new Reader(context.Model.Postprocessing.TextLogParser, CancellationToken.None)).Read(() => testStream, _ => { }).ToFlatList();

				var parsedMessage = LogJoint.Chromium.ChromeDriver.DevTools.Events.LogMessage.Parse(messages[1].Text);
				Check.That(parsedMessage.EventType.Value).IsEqualTo("loadingFinished");
				Check.That(parsedMessage.EventNamespace.Value).IsEqualTo("Network");

				var parsedPayload = parsedMessage.ParsePayload<LogJoint.Chromium.ChromeDriver.DevTools.Events.Network.LoadingFinished>();
				Check.That(parsedPayload).IsNotNull();
				Check.That(parsedPayload.requestId).IsEqualTo("27949.24");

				var parsedTimeStampsPayload = parsedMessage.ParsePayload<LogJoint.Chromium.ChromeDriver.DevTools.Events.TimeStampsInfo>();
				Check.That(parsedTimeStampsPayload).IsNotNull();
				Check.That(parsedTimeStampsPayload.timestamp.Value).IsCloseTo(597454.928244, 1e-10);
			}
		}
	}
}
