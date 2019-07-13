using M = LogJoint.PacketAnalysis;
using V = LogJoint.PacketAnalysis.UI;
using P = LogJoint.PacketAnalysis.UI.Presenters;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			M.PluginInitializer.Init(app, new ViewsFactory { app = app });
		}

		class ViewsFactory : P.Factory.IViewsFactory
		{
			public IApplication app;

			P.MessagePropertiesDialog.IView P.Factory.IViewsFactory.CreateMessageContentView()
			{
				return new V.MessageContentViewController(app.View);
			}

			P.NewLogSourceDialog.Pages.WiresharkPage.IView P.Factory.IViewsFactory.CreateWiresharkPageView()
			{
				return new V.WiresharkPageAdapter();
			}
		};
	}
}
