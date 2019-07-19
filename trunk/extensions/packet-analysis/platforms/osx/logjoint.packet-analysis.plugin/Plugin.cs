using M = LogJoint.PacketAnalysis;
using V = LogJoint.PacketAnalysis.UI;
using P = LogJoint.PacketAnalysis.UI.Presenters;
using Foundation;
using System.Reflection;
using System.IO;
using System;
using System.Threading;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			M.PluginInitializer.Init(app, new ViewsFactory(app));
		}

		class ViewsFactory : P.Factory.IViewsFactory
		{
			private readonly IApplication app;
			private readonly Lazy<NSBundle> bundle;

			public ViewsFactory(IApplication app)
			{
				this.app = app;
				this.bundle = new Lazy<NSBundle>(
					() => NSBundle.FromPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)),
					LazyThreadSafetyMode.None
				);
			}

			P.MessagePropertiesDialog.IView P.Factory.IViewsFactory.CreateMessageContentView()
			{
				return new V.MessageContentViewController(app.View, bundle.Value);
			}

			P.NewLogSourceDialog.Pages.WiresharkPage.IView P.Factory.IViewsFactory.CreateWiresharkPageView()
			{
				return new V.WiresharkPageAdapter(bundle.Value);
			}
		};
	}
}
