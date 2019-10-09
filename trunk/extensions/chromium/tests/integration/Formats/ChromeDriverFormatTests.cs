using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Chromium;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System.IO;
using LogJoint.Chromium.ChromeDriver;
using LogJoint.Postprocessing;
using System.Threading;

namespace LogJoint.Tests.Integration.Chromium
{
	[TestFixture]
	class ChromeDriverFormatTests
	{
		SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();
			Factory.Create(app.Model.ExpensibilityEntryPoint);
		}

		[TearDown]
		public async Task AfterEach()
		{
			await app.Dispose();
		}

		[Test]
		public async Task LoadsLogFileAndEnablesPostprocessors()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("chromedriver_1.log"));

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				Assert.AreEqual("[1548250986.197][INFO]: Waiting for pending navigations...", app.ViewModel.LoadedMessagesLogViewer.ViewLines[2].TextLineValue);
				app.ViewModel.MainForm.OnTabChanging(app.ViewModel.PostprocessingTabPageId);
				var postprocessorsControls = app.ViewModel.PostprocessingTabPage.ControlsState;
				Assert.IsFalse(postprocessorsControls[ViewControlId.Timeline].Disabled);

				return 0;
			});
		}

		[Test, Category("SplitAndCompose")]
		public async Task ChromeDriver_SplitAndComposeTest()
		{
			using (var testStream = await samples.GetSampleAsStream("chromedriver_2019_01_23.log"))
			{
				var actualContent = new MemoryStream();

				var reader = new Reader(new TextLogParser(), CancellationToken.None);
				var writer = new Writer();

				await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

				Utils.AssertTextsAreEqualLineByLine(
					Utils.SplitTextStream(testStream),
					Utils.SplitTextStream(actualContent)
				);
			}
		}

		[Test, Category("SplitAndCompose")]
		public async Task ChromeDriver_SplitAndCompose_WithForeignLogging()
		{
			using (var testStream = await samples.GetSampleAsStream("chromedriver_2019_01_22.log"))
			{
				var actualContent = new MemoryStream();

				var reader = new Reader(new TextLogParser(), CancellationToken.None);
				var writer = new Writer();

				await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

				Utils.AssertTextsAreEqualLineByLine(
					Utils.SplitTextStream(testStream),
					Utils.SplitTextStream(actualContent)
				);
			}
		}

		[Test]
		public async Task ForeignLoggingAtEndOfMssagesIsIgnored()
		{
			using (var testStream = await samples.GetSampleAsStream("chromedriver_2019_01_22.log"))
			{
				var messages = await (new Reader(new TextLogParser(), CancellationToken.None)).Read(() => testStream, _ => { }).ToFlatList();

				var parsedMessage = LogJoint.Chromium.ChromeDriver.DevTools.Events.LogMessage.Parse(messages[1].Text);
				Assert.AreEqual("loadingFinished", parsedMessage.EventType);
				Assert.AreEqual("Network", parsedMessage.EventNamespace);

				var parsedPayload = parsedMessage.ParsePayload<LogJoint.Chromium.ChromeDriver.DevTools.Events.Network.LoadingFinished>();
				Assert.IsNotNull(parsedPayload);
				Assert.AreEqual("27949.24", parsedPayload.requestId);

				var parsedTimeStampsPayload = parsedMessage.ParsePayload<LogJoint.Chromium.ChromeDriver.DevTools.Events.TimeStampsInfo>();
				Assert.IsNotNull(parsedTimeStampsPayload);
				Assert.AreEqual(597454.928244, parsedTimeStampsPayload.timestamp.Value, 1e-10);
			}
		}
	}
}
