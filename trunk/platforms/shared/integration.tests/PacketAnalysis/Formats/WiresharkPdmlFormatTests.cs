using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PA = LogJoint.PacketAnalysis;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;

namespace LogJoint.Tests.Integration.PacketAnalysis
{
	[TestFixture]
	class WiresharkPdmlFormatTests
	{
		SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;
		PA.UI.Presenters.Factory.IViewsFactory viewsFactory;

		[SetUp]
		public async Task BeforeEach()
		{
			viewsFactory = Substitute.For<PA.UI.Presenters.Factory.IViewsFactory>();
			app = await TestAppInstance.Create();
			PA.UI.Presenters.Factory.Create(
				PA.Factory.Create(app.Model.ExpensibilityEntryPoint),
				app.Presentation.ExpensibilityEntryPoint,
				app.Model.ExpensibilityEntryPoint,
				viewsFactory
			);
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
				await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("network_trace_with_keys_1.tar.gz"));

				await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);

				// todo: asserts

				return 0;
			});
		}
	}
}
