using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogSourcesController
	{
		ILogSource CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams);
		Task DeleteAllLogsAndPreprocessings();
	};
}
