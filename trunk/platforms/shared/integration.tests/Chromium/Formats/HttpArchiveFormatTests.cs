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
	class HttpArchiveFormatTests
	{
		PluginLoader pluginLoader = new PluginLoader();
		SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		[SetUp]
		public async Task BeforeEach()
		{
			app = await TestAppInstance.Create();
			app.Model.PluginFormatsManager.RegisterPluginFormats(pluginLoader.Manifest);
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
	}
}
