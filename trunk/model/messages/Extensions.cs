namespace LogJoint
{
	public static class MessageExtentions
	{
		public static StringUtils.MultilineText GetText(this IMessage msg)
		{
			return msg.TextAsMultilineText;
		}

		public static StringUtils.MultilineText GetRawText(this IMessage msg)
		{
			var r = msg.RawTextAsMultilineText;
			if (r.Text.IsInitialized)
				return r;
			return msg.TextAsMultilineText;
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

		public static bool TryGetLogSource(this IMessage message, out ILogSource ls)
		{
			ls = message.GetLogSource();
			return ls != null;
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
