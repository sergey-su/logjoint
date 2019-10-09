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

namespace LogJoint.Tests.Integration.Chromium
{
	[TestFixture]
	class HttpArchiveFormatTests
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
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("www.hemnet.se.har"));

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				Assert.AreEqual("entry#147 I header  date: Sat, 13 Jul 2019 13:36:59 GMT", app.ViewModel.LoadedMessagesLogViewer.ViewLines[3].TextLineValue);
				app.ViewModel.MainForm.OnTabChanging(app.ViewModel.PostprocessingTabPageId);
				var postprocessorsControls = app.ViewModel.PostprocessingTabPage.ControlsState;
				Assert.IsFalse(postprocessorsControls[ViewControlId.Timeline].Disabled);
				Assert.IsFalse(postprocessorsControls[ViewControlId.Sequence].Disabled);

				return 0;
			});
		}

		[Test]
		public async Task CanLoadBrokenHarFile()
		{
			await app.SynchronizationContext.InvokeAndAwait(async () =>
			{
				var tempHarFileName = Path.Combine(app.AppDataDirectory, "broken.har");
				var harContent = File.ReadAllText(await samples.GetSampleAsLocalFile("www.hemnet.se.har"));
				harContent = harContent.Substring(0, harContent.Length - 761); // break HAR json by randomly cutting the tail
				File.WriteAllText(tempHarFileName, harContent);
				await app.EmulateFileDragAndDrop(tempHarFileName);

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				Assert.AreEqual("entry#147 I receive http/2.0+quic/46 200", app.ViewModel.LoadedMessagesLogViewer.ViewLines[1].TextLineValue);

				return 0;
			});
		}

	}
}
