using LogJoint.Symphony;

namespace LogJoint
{
	public class Plugin: IPluginStartup
	{
		readonly PluginImpl impl;

		public Plugin(IApplication app)
		{
			impl = new PluginImpl(app);
		}

		public void Start()
		{
			impl.Start();
		}
	}
}
