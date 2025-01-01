using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

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

        public static bool LogSourceStateIsOkToChangePersistentState(this ILogSource s)
        {
            if (s == null || s.IsDisposed)
                return false;
            if (s.Provider == null || s.Provider.IsDisposed)
                return false;
            var state = s.Provider.Stats.State;
            if (state == LogProviderState.LoadError || state == LogProviderState.NoFile)
                return false;
            return true;
        }
    }
}
