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

		[BeforeAll]
		public async Task BeforeEach(IContext context)
		{
			viewsFactory = context.Registry.Get<PA.UI.Presenters.Factory.IViewsFactory>();
			viewsFactory.CreateMessageContentView().SetViewModel(
				Arg.Do<PA.UI.Presenters.MessagePropertiesDialog.IViewModel>(x => messagePropertiesViewModel = x));
			messagePropertiesOSView = new object();
			viewsFactory.CreateMessageContentView().OSView.Returns(messagePropertiesOSView);

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
		public async Task MessagePropertiesDialogExtensionIsRegistered(IContext context)
		{
			await context.Presentation.LoadedMessagesLogViewer.GoToEnd();
			Check.That(context.Presentation.LoadedMessagesLogViewer.FocusedMessage).IsNotNull();

			var dlg = context.Presentation.MessagePropertiesDialog;
			dlg.Show();

			Check.That(dlg.ContentViewModes.Count).IsEqualTo(3);
			Check.That(dlg.ContentViewModes[2]).IsEqualTo("Packet protocols");

			dlg.SelectedContentViewMode = 2;
			Check.That(dlg.SelectedContentViewMode).IsEqualTo(2);

			Check.That(messagePropertiesViewModel.Root.Children.Count).IsEqualTo(4);
		}
	};
}
