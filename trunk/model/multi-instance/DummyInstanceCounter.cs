
namespace LogJoint.MultiInstance
{
    public class DummyInstancesCounter : IInstancesCounter
    {
        bool IInstancesCounter.IsPrimaryInstance => false;

        string IInstancesCounter.MutualExecutionKey => "";

        int IInstancesCounter.Count => 1;
    };
}
