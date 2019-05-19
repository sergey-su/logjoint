using LogJoint.Chromium;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			PluginInitializer.Init(app);
		}
	}
}
