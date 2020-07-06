using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	public interface IDisposableAsync  // todo: move out of Postprocessing
	{
		Task Dispose();
	};

	public interface IEnumeratorAsync<out T> : IDisposableAsync
	{
		T Current { get; }
		Task<bool> MoveNext();
	};

	public interface IEnumerableAsync<T>
	{
		Task<IEnumeratorAsync<T>> GetEnumerator();
	};

	public interface IYieldAsync<T>
	{
		Task<bool> YieldAsync(T value);
	};

	public interface IMultiplexingEnumerableOpen
	{
		Task Open();
	}

	public interface IMultiplexingEnumerable<T> : IEnumerableAsync<T>, IMultiplexingEnumerableOpen
	{
	};
}
