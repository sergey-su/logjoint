using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LogJoint.RegularExpressions;

namespace LogJoint
{
    [DebuggerDisplay("Time={Time}, Position={Position}")]
    public class Bookmark : IBookmark
    {
        public Bookmark(MessageTimestamp time, IThread thread, string displayName, long position, int lineIndex) :
            this(time, thread, thread != null && !thread.IsDisposed && thread.LogSource != null ? thread.LogSource.Provider.ConnectionId : "", displayName, position, lineIndex)
        { }
        public Bookmark(MessageTimestamp time, string sourceConnectionId, long position, int lineIndex) :
            this(time, null, sourceConnectionId, "", position, lineIndex)
        { }
        public Bookmark(IMessage msg, int lineIndex, bool useRawText)
            : this(msg.Time, msg.Thread,
                MakeDisplayName(msg, lineIndex, useRawText), msg.Position, lineIndex)
        { }

        MessageTimestamp IBookmark.Time { get { return time; } }
        IThread IBookmark.Thread { get { return thread; } }
        string IBookmark.LogSourceConnectionId { get { return logSourceConnectionId; } }
        long IBookmark.Position { get { return position; } }
        int IBookmark.LineIndex { get { return lineIndex; } }
        string IBookmark.DisplayName { get { return displayName; } }
        IBookmark IBookmark.Clone()
        {
            return new Bookmark(time, thread, logSourceConnectionId, displayName, position, lineIndex);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", time.ToUserFrendlyString(showMilliseconds: true), displayName ?? "");
        }

        internal Bookmark(MessageTimestamp time, IThread thread, string logSourceConnectionId,
            string displayName, long position, int lineIndex)
        {
            this.time = time;
            this.thread = thread;
            this.displayName = displayName;
            this.position = position;
            this.logSourceConnectionId = logSourceConnectionId;
            this.lineIndex = lineIndex;
        }

        static string MakeDisplayName(IMessage msg, int lineIndex, bool useRawText)
        {
            var txt = useRawText && msg.RawText.IsInitialized ? msg.RawTextAsMultilineText : msg.TextAsMultilineText;
            var ret = txt.GetNthTextLine(lineIndex).Value;
            if (ret.Length != 0)
                return ret;
            return txt.Text.Value;
        }

        readonly MessageTimestamp time;
        readonly IThread thread;
        readonly string logSourceConnectionId;
        readonly long position;
        readonly int lineIndex;
        readonly string displayName;
    }
}
