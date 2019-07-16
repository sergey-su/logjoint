using LogJoint.Symphony;

namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IApplication app)
		{
			new PluginImpl(app);
		}
	}
}
