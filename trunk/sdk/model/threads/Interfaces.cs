using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint
{
	public interface IThread : IDisposable
	{
		bool IsDisposed { get; }
		string ID { get; }
		string Description { get; }
		string DisplayName { get; }
		bool ThreadMessagesAreVisible { get; }
		int ThreadColorIndex { get; }
		IBookmark FirstKnownMessage { get; }
		IBookmark LastKnownMessage { get; }
		/// <summary>
		/// Thread-safe. Can be gotten on disposed object.
		/// </summary>
		ILogSource LogSource { get; }
	}

	public struct ThreadsBulkProcessingResult
	{
	};

	public interface IThreadsBulkProcessing : IDisposable
	{
		ThreadsBulkProcessingResult ProcessMessage(IMessage message);
	};

	public interface IModelThreads
	{
		event EventHandler OnThreadListChanged;
		event EventHandler OnThreadVisibilityChanged;
		event EventHandler OnPropertiesChanged;
		IEnumerable<IThread> Items { get; }
		IThreadsBulkProcessing StartBulkProcessing();

		IThread RegisterThread(string id, ILogSource logSource);
	};

	public interface ILogSourceThreads : IDisposable
	{
		IModelThreads UnderlyingThreadsContainer { get; }
		IEnumerable<IThread> Items { get; }
		IThread GetThread(StringSlice id);
		void DisposeThreads();
	};
}
