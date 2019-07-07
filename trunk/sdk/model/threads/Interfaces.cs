using System;
using System.Collections.Generic;

namespace LogJoint
{
	/// <summary>
	/// Represents message thread information parsed-out from a log.
	/// Messages from same thread share the reference to same <see cref="IThread"/> object.
	/// Threading: all members must be used from model context.
	/// </summary>
	public interface IThread
	{
		/// <summary>
		/// Removed this thread from <see cref="IModelThreads"/> list.
		/// </summary>
		void Dispose();
		bool IsDisposed { get; }
		string ID { get; }
		string Description { get; }
		string DisplayName { get; }
		int ThreadColorIndex { get; }
		IBookmark FirstKnownMessage { get; }
		IBookmark LastKnownMessage { get; }
		/// <summary>
		/// Can be gotten on disposed object.
		/// </summary>
		ILogSource LogSource { get; }
		/// <summary>
		/// Notifies that a log message was seen to belong to this thread.
		/// No-op for disposed threads.
		/// </summary>
		void RegisterKnownMessage(IMessage message);
	}

	/// <summary>
	/// Contains flat list of all log message threads (<see cref="IThread"/>) from all currently open log sources.
	/// Threading: mixed, see individual members.
	/// </summary>
	public interface IModelThreads
	{
		/// <summary>
		/// Fired when threads list changed. <see cref="Items"/>.
		/// Threading: called from unspecified thread.
		/// </summary>
		event EventHandler OnThreadListChanged;
		/// <summary>
		/// Notification that thread one of many <see cref="IThread"/> properties have changed.
		/// Threading: called from unspecified thread.
		/// </summary>
		event EventHandler OnThreadPropertiesChanged;
		/// <summary>
		/// Returns an immutable copy of current threads list.
		/// Threading: thread-safe
		/// </summary>
		IReadOnlyList<IThread> Items { get; }

		/// <summary>
		/// Allocates a new thread and adds it to the list.
		/// Threading: can be called from any thread.
		/// </summary>
		IThread RegisterThread(string id, ILogSource logSource);
	};

	/// <summary>
	/// Contains list of all log message threads for one log source.
	/// Threading: mixed, see individual members.
	/// </summary>
	public interface ILogSourceThreads: IDisposable
	{
		/// <summary>
		/// Returns an immutable snapshot of current list.
		/// Thread-safe.
		/// </summary>
		IReadOnlyList<IThread> Items { get; }
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
