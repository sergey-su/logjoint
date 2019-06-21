using LogJoint.PacketAnalysis;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			PluginInitializer.Init(app, 
				new LogJoint.PacketAnalysis.UI.WiresharkPageAdapter(),
				() => new LogJoint.PacketAnalysis.UI.MessageContentViewController(app.View)
			);
		}
	}
}
