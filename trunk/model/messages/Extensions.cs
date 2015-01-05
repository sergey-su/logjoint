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

		public static bool IsHiddenBecauseOfInvisibleThread(this IMessage message)
		{
			return (message.Flags & MessageFlag.HiddenBecauseOfInvisibleThread) != 0;
		}

		public static bool IsHighlighted(this IMessage message)
		{
			return (message.Flags & MessageFlag.IsHighlighted) != 0;
		}

		public static bool IsStartFrame(this IMessage message)
		{
			return (message.Flags & MessageFlag.TypeMask) == MessageFlag.StartFrame;
		}

		public static int GetLinesCount(this IMessage message)
		{
			return message.TextAsMultilineText.GetLinesCount();
		}

		public static StringSlice GetNthTextLine(this IMessage message, int lineIdx)
		{
			return message.TextAsMultilineText.GetNthTextLine(lineIdx);
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
	};
}
