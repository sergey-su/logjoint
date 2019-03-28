using LogJoint.Extensibility;
using LogJoint.PacketAnalysis;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		public Plugin()
		{
		}

		public override void Init(IApplication app)
		{
			PluginInitializer.Init(app, new LogJoint.PacketAnalysis.UI.WiresharkPageAdapter());
		}


		public override void Dispose()
		{
		}
	}
}
