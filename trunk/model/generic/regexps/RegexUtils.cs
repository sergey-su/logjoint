using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.RegularExpressions
{
    public static class RegexUtils
    {
        public static bool IsMatch(this IRegex re, StringSlice slice, int startFrom = 0)
        {
            var m = re.CreateEmptyMatch();
            return re.Match(slice.Buffer, slice.StartIndex + startFrom, slice.Length - startFrom, ref m);
        }

        public static StringSlice ToStringSlice(this Group g, string buffer)
        {
            return new StringSlice(buffer, g.Index, g.Length);
        }

        public static string ToString(this Group g, string buffer)
        {
            return g.ToStringSlice(buffer).ToString();
        }

        public static System.Text.RegularExpressions.RegexOptions GetCompiledOptionIfAvailable()
        {
#if !SILVERLIGHT
            return System.Text.RegularExpressions.RegexOptions.Compiled;
#else
			return System.Text.RegularExpressions.RegexOptions.None;
#endif
        }

        public static IRegex CloneRegex(IRegex re, ReOptions optionsToAdd = ReOptions.None)
        {
            if (re != null)
                return re.Factory.Create(re.Pattern, re.Options | optionsToAdd);
            else
                return null;
        }
    }
}
