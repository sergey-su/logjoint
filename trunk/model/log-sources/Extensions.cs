using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public static class LogSourceExtensions
	{
		public static string GetShortDisplayNameWithAnnotation(this ILogSource ls)
		{
			if (ls.IsDisposed)
				return "";
			var logSourceName = ls.DisplayName;
			try
			{
				logSourceName = Path.GetFileName(logSourceName); // try to shorten long path
			}
			catch (ArgumentException)
			{
			}
			if (!string.IsNullOrEmpty(ls.Annotation))
				return ls.Annotation + "  " + logSourceName;
			return logSourceName;
		}

		public static string GetSafeConnectionId(this ILogSource ls)
		{
			if (ls.IsDisposed)
				return "";
			return ls.Provider.ConnectionId;
		}

		public static ILogSource FindLiveLogSourceOrCreateNew(
			this ILogSourcesManager logSources,
			ILogProviderFactory factory, 
			IConnectionParams cp)
		{
			ILogSource src = logSources.Find(cp);
			if (src != null && src.Provider.Stats.State == LogProviderState.LoadError)
			{
				src.Dispose();
				src = null;
			}
			if (src == null)
			{
				src = logSources.Create(factory, cp);
			}
			return src;
		}

		public static async Task<IBookmark> CreateTogglableBookmark(
			this ILogSource ls, 
			IBookmarksFactory factory,
			IBookmark sourceBookmark,
			CancellationToken cancallation
		)
		{
			if (sourceBookmark.LogSourceConnectionId != ls.Provider.ConnectionId)
				throw new ArgumentException("log source and bookmark have inconsistent connection ids");
			IMessage messageAtPosition = null;
			await ls.Provider.EnumMessages(
				sourceBookmark.Position,
				msg => 
				{
					if (msg.Position == sourceBookmark.Position)
						messageAtPosition = msg;
					return false;
				},
				EnumMessagesFlag.Forward,
				LogProviderCommandPriority.RealtimeUserAction,
				cancallation
			);
			if (messageAtPosition == null)
				return null;
			return factory.CreateBookmark(messageAtPosition, sourceBookmark.LineIndex, true);
		}

		public static async Task DeleteLogs(this ILogSourcesManager lsm, ILogSource[] sources)
		{
			var tasks = sources.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteAllLogs(this ILogSourcesManager lsm)
		{
			await DeleteLogs(lsm, lsm.Items.ToArray());
		}
	}
}
