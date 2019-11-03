
namespace LogJoint.MultiInstance
{
	public interface IInstancesCounter
	{
		bool IsPrimaryInstance { get; }
		string MutualExecutionKey { get; }
		int Count { get; }
	};
}
