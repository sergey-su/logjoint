using NFluent;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PA = LogJoint.PacketAnalysis;

namespace LogJoint.Tests.Integration.PacketAnalysis
{
	[IntegrationTestFixture]
	class WiresharkPdmlFormatTests
	{
		PA.UI.Presenters.Factory.IViewsFactory viewsFactory;
		PA.UI.Presenters.MessagePropertiesDialog.IViewModel messagePropertiesViewModel;
		object messagePropertiesOSView;

		[BeforeEach]
		public async Task BeforeEach(IContext context)
		{
			viewsFactory = context.Registry.Get<PA.UI.Presenters.Factory.IViewsFactory>();
			viewsFactory.CreateMessageContentView().SetViewModel(
				Arg.Do<PA.UI.Presenters.MessagePropertiesDialog.IViewModel>(x => messagePropertiesViewModel = x));
			messagePropertiesOSView = new object();
			viewsFactory.CreateMessageContentView().OSView.Returns(messagePropertiesOSView);

			// todo: support BeforeAll
			await context.Utils.EmulateFileDragAndDrop(await context.Samples.GetSampleAsLocalFile("network_trace_with_keys_3.tar.gz"));
			await context.Utils.WaitFor(() => context.Presentation.LoadedMessagesLogViewer.VisibleLines.Count > 0);
		}


		[IntegrationTest]

		public void FormatIsDetectedAndLoaded(IContext context)
		{
			Check.That(context.Presentation.LoadedMessagesLogViewer.VisibleLines[0].Value)
				.IsEqualTo("        IP Option - Router Alert (4 bytes): Router shall examine packet (0)");
		}

		[IntegrationTest]

		public void PostprocessorsEnabled(IContext context)
		{
			Check.That(context.Presentation.Postprocessing.SummaryView.Timeline.Enabled).IsTrue();
		}

		[IntegrationTest]
		public void MessagePropertiesDialogExtensionIsRegistered(IContext context)
		{
			var dlg = context.Presentation.MessagePropertiesDialog;
			dlg.ShowDialog();

			/*Assert.AreEqual(3, dlg.Data.ContentViewModes.Count); todo
			Assert.AreEqual("Packet protocols", dlg.Data.ContentViewModes[2]);

			dlg.OnContentViewModeChange(2);
			Assert.AreEqual(2, dlg.Data.ContentViewModeIndex);
			Assert.IsNotNull(dlg.Data.CustomView);
			Assert.AreSame(messagePropertiesOSView, dlg.Data.CustomView);

			Assert.AreEqual(4, messagePropertiesViewModel.Root.Children.Count);*/
		}
	};
}
