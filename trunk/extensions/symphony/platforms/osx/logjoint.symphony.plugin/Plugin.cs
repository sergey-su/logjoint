using LogJoint.Symphony;

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
