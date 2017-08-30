using System.Linq;

namespace LogJoint
{
	public class BookmarkController : IBookmarksController
	{
		readonly LazyUpdateFlag bookmarksNeedPurgeFlag = new LazyUpdateFlag();
		readonly LJTraceSource tracer;

		public BookmarkController(
			IBookmarks bookmarks,
			IModelThreads threads,
			IHeartBeatTimer heartbeat
		)
		{
			tracer = LJTraceSource.EmptyTracer;
			threads.OnThreadListChanged += (s, e) => 
			{
				bookmarksNeedPurgeFlag.Invalidate();
			};
			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && bookmarksNeedPurgeFlag.Validate())
					bookmarks.PurgeBookmarksForDisposedThreads();
			};
			bookmarks.OnBookmarksChanged += (sender, e) => 
			{
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
							affectedSource.StoreBookmarks();
						}
						catch (Persistence.StorageException storageException)
						{
							tracer.Error(storageException, "Failed to store bookmarks for log {0}", 
								affectedSource.GetSafeConnectionId());
						}
					}
				}
			};
		}
	};
}
