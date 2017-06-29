using LogJoint.Extensibility;
using LogJoint.Chromium;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		public Plugin()
		{
		}

		public override void Init(IApplication app)
		{
			PluginInitializer.Init(app);
		}

		public override void Dispose()
		{
		}
	}
}
