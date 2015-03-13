using System;
using System.Collections.Generic;
using System.IO;

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
			return ls.ConnectionId;
		}
	}
}
