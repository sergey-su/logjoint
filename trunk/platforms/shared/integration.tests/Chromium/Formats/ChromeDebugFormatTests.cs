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

namespace LogJoint.Tests.Integration.Chromium
{
	[TestFixture]
	class ChromeDebugTests
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
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("chrome_debug_1.log"));

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				Assert.AreEqual("ProcessThreadAttached 0x(nil)", app.ViewModel.LoadedMessagesLogViewer.ViewLines[0].TextLineValue);
				app.ViewModel.MainForm.OnTabChanging(app.ViewModel.PostprocessingTabPageId);
				var postprocessorsControls = app.ViewModel.PostprocessingTabPage.ControlsState;
				Assert.IsFalse(postprocessorsControls[ViewControlId.Timeline].Disabled);
				Assert.IsFalse(postprocessorsControls[ViewControlId.StateInspector].Disabled);
				Assert.IsFalse(postprocessorsControls[ViewControlId.TimeSeries].Disabled);

				return 0;
			});
		}
	}
}
