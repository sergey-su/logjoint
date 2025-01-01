using System.Threading.Tasks;

namespace LogJoint
{
    public interface ILogSourcesController
    {
        Task DeleteAllLogsAndPreprocessings();
    };
}
