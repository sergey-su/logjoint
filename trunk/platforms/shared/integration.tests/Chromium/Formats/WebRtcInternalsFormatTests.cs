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
	class WebRtcInternalsFormatTests
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
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("webrtc_internals_dump_1.txt"));

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				Assert.AreEqual("2017-06-30T18:02:21.000000|C|35286-1|log|addIceCandidate|sdpMid: audio, sdpMLineIndex: 0, candidate: candidate:508100464 1 udp 2122260223 192.168.10.157 57279 typ host generation 0 ufrag yKWx network-id 1 network-cost 10", app.ViewModel.LoadedMessagesLogViewer.ViewLines[0].TextLineValue);
				app.ViewModel.MainForm.OnTabChanging(app.ViewModel.PostprocessingTabPageId);
				var postprocessorsControls = app.ViewModel.PostprocessingTabPage.ControlsState;
				Assert.IsFalse(postprocessorsControls[ViewControlId.StateInspector].Disabled);
				Assert.IsFalse(postprocessorsControls[ViewControlId.TimeSeries].Disabled);

				return 0;
			});
		}
	}
}
