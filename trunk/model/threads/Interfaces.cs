using System;

namespace LogJoint
{
	public interface IModelThreadsInternal : IModelThreads
	{
		/// <summary>
		/// Allocates a new thread and adds it to the list.
		/// Threading: can be called from any thread.
		/// </summary>
		IThread RegisterThread(string id, ILogSource logSource);
		/// <summary>
		/// Removed the thread from the list.
		/// Threading: can be called from any thread.
		/// </summary>
		void UnregisterThread(IThread thread);
	};

	/// <summary>
	/// Threading: mixed, see individual members.
	/// </summary>
	public interface ILogSourceThreadsInternal : ILogSourceThreads, IDisposable
	{
		/// <summary>
		/// Gets existing thread object or allocates new.
		/// Thread-safe.
		/// </summary>
		IThread GetThread(StringSlice id);
		/// <summary>
		/// Disposes and removes all threads from the list.
		/// Threading: must be called in model context if <see cref="IThread"/> objects are reachable from model context,
		/// otherwise can be disposed from any thread. Same threading rules apply to Dispose().
		/// </summary>
		void Clear();
	};
}
