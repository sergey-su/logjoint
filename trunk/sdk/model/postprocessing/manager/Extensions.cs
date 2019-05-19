using LogJoint.Analytics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	public static class Extensions
	{
		public static string GetLogFileNameHint(this ILogProvider provider)
		{
			if (!(provider is ISaveAs saveAs) || !saveAs.IsSavableAs)
				return null;
			return saveAs.SuggestedFileName;
		}

		public static string GetLogFileNameHint(this LogSourcePostprocessorInput input)
		{
			return GetLogFileNameHint(input.LogSource.Provider);
		}
	}
}
