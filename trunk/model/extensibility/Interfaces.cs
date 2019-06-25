
namespace LogJoint
{
	public interface IPluginsManagerStarup: IPluginsManager
	{
		void LoadPlugins(object appEntryPoint);
	}
}
