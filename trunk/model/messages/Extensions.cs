using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public static class MessageExtentions
	{
		public static bool IsVisible(this IMessage message)
		{
			return (message.Flags & MessageFlag.HiddenAll) == 0;
		}

		public static bool IsHiddenAsFilteredOut(this IMessage message)
		{
			return (message.Flags & MessageFlag.HiddenAsFilteredOut) != 0;
		}

		public static bool IsHighlighted(this IMessage message)
		{
			return (message.Flags & MessageFlag.IsHighlighted) != 0;
		}

		public static bool IsStartFrame(this IMessage message)
		{
			return (message.Flags & MessageFlag.TypeMask) == MessageFlag.StartFrame;
		}

		public static StringUtils.MultilineText GetDisplayText(this IMessage msg, bool displayRawTextMode)
		{
			if (displayRawTextMode)
			{
				var r = msg.RawTextAsMultilineText;
				if (r.Text.IsInitialized)
					return r;
				return msg.TextAsMultilineText;
			}
			else
			{
				return msg.TextAsMultilineText;
			}
		}

		/// <summary>
		/// Returns not disposed log source that given message belongs to.
		/// null if message is not associated with any log source or log source is disposed.
		/// </summary>
		public static ILogSource GetLogSource(this IMessage message)
		{
			var thread = message.Thread;
			if (thread == null || thread.IsDisposed)
				return null;
			var ls = thread.LogSource;
			if (ls == null || ls.IsDisposed)
				return null;
			return ls;
		}

		/// <summary>
		/// Return connection id of message's log source. Empty string if log source is not set or disposed.
		/// </summary>
		public static string GetConnectionId(this IMessage msg)
		{
			var ls = msg.GetLogSource();
			return ls != null ? ls.Provider.ConnectionId : "";
		}
	};
}
