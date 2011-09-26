using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.RegularExpressions
{
	public static class RegexUtils
	{
		public static bool Match(this IRegex re, StringSlice slice, int startFrom, ref IMatch returnMatch)
		{
			return re.Match(slice.Buffer, slice.StartIndex + startFrom, slice.Length - startFrom, ref returnMatch);
		}

		public static System.Text.RegularExpressions.RegexOptions GetCompiledOptionIfAvailable()
		{
#if !SILVERLIGHT
			return System.Text.RegularExpressions.RegexOptions.Compiled;
#else
			return System.Text.RegularExpressions.RegexOptions.None;
#endif
		}
	}
}
