using System.Threading.Tasks;

namespace LogJoint
{
	public interface IShutdownSource: IShutdown
	{
		Task Shutdown();
	}
}
