using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public class BookmarkController : IBookmarksController
	{
		readonly AsyncInvokeHelper bookmarksPurge;
		readonly LJTraceSource tracer;
		Task bookmarksSaveTask = Task.CompletedTask;

		public BookmarkController(
			IBookmarks bookmarks,
			IModelThreads threads,
			ISynchronizationContext synchronization,
			IShutdown shutdown
		)
		{
			tracer = LJTraceSource.EmptyTracer;
			bookmarksPurge = new AsyncInvokeHelper(synchronization, bookmarks.PurgeBookmarksForDisposedThreads);
			threads.OnThreadListChanged += (s, e) => 
			{
				bookmarksPurge.Invoke();
			};
			bookmarks.OnBookmarksChanged += (sender, e) => 
			{
				bookmarksSaveTask = HandleEvent(bookmarksSaveTask, e, tracer);
			};
			shutdown.Cleanup += (sender, e) => shutdown.AddCleanupTask(bookmarksSaveTask);
		}

		static async Task HandleEvent(Task queueTask, BookmarksChangedEventArgs e, LJTraceSource tracer)
        {
			await queueTask;
			if (e.Type == BookmarksChangedEventArgs.ChangeType.Added || e.Type == BookmarksChangedEventArgs.ChangeType.Removed ||
				e.Type == BookmarksChangedEventArgs.ChangeType.RemovedAll || e.Type == BookmarksChangedEventArgs.ChangeType.Purged)
			{
				foreach (var affectedSource in
					e.AffectedBookmarks
					.Select(b => b.GetLogSource())
					.Where(s => s.LogSourceStateIsOkToChangePersistentState())
					.Distinct())
				{
					try
					{
						await affectedSource.StoreBookmarks();
					}
					catch (Persistence.StorageException storageException)
					{
						tracer.Error(storageException, "Failed to store bookmarks for log {0}",
							affectedSource.GetSafeConnectionId());
					}
				}
			}
		}
	};
}
