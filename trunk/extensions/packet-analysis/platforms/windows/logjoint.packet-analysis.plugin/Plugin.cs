using LogJoint.PacketAnalysis;
using P = LogJoint.PacketAnalysis.UI.Presenters;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			PluginInitializer.Init(app, new ViewsFactory { app = app });
		}

		class ViewsFactory: P.Factory.IViewsFactory
		{
			public IApplication app;

			P.MessagePropertiesDialog.IView P.Factory.IViewsFactory.CreateMessageContentView()
			{
				return new PacketAnalysis.UI.MessageContentView(app.View);
			}

			P.NewLogSourceDialog.Pages.WiresharkPage.IView P.Factory.IViewsFactory.CreateWiresharkPageView()
			{
				return new P.NewLogSourceDialog.Pages.WiresharkPage.WiresharkPageUI();
			}
		}
	}
}
