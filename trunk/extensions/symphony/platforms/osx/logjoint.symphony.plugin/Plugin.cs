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

		void IPluginStartup.Start()
		{
			impl.Start();
		}
	}
}
