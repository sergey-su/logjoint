namespace LogJoint
{
	public interface IPluginsManager
	{
		void Register<PluginType>(PluginType plugin) where PluginType : class;
		PluginType Get<PluginType>() where PluginType: class;
 	};

	public interface IPluginStartup
	{
		void Start();
	};
}
