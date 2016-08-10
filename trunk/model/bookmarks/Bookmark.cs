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
		public Bookmark(MessageTimestamp time, IThread thread, string displayName, string messageText, long position) :
			this(time, thread, thread != null && !thread.IsDisposed && thread.LogSource != null ? thread.LogSource.ConnectionId : "", displayName, messageText, position)
		{ }
		public Bookmark(MessageTimestamp time, string sourceCollectionId, long position) :
			this(time, null, sourceCollectionId, "", "", position)
		{ }
		public Bookmark(IMessage line)
			: this(line.Time, line.Thread, line.Text.Value, line.RawText.IsInitialized ? line.RawText.Value : line.Text.Value, line.Position)
		{ }
		public Bookmark(MessageTimestamp time)
			: this(time, null, null, null, 0)
		{ }

		MessageTimestamp IBookmark.Time { get { return time; } }
		IThread IBookmark.Thread { get { return thread; } }
		string IBookmark.LogSourceConnectionId { get { return logSourceConnectionId; } }
		long IBookmark.Position { get { return position; } }
		string IBookmark.DisplayName { get { return displayName; } }
		string IBookmark.MessageText { get { return messageText; } }
		IBookmark IBookmark.Clone()
		{
			return new Bookmark(time, thread, logSourceConnectionId, displayName, messageText, position);
		}

		public override string ToString()
		{
			return string.Format("{0} {1}", time.ToUserFrendlyString(showMilliseconds: true), displayName ?? "");
		}

		internal Bookmark(MessageTimestamp time, IThread thread, string logSourceConnectionId, string displayName, string messageText, long position)
		{
			this.time = time;
			this.thread = thread;
			this.displayName = displayName;
			this.messageText = messageText;
			this.position = position;
			this.logSourceConnectionId = logSourceConnectionId;
		}

		MessageTimestamp time;
		IThread thread;
		string logSourceConnectionId;
		long position;
		string displayName;
		string messageText;
	}
}
