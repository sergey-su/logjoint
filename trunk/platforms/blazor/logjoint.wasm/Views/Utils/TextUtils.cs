using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogJoint.Wasm.UI
{
	public static class TextUtils
	{
		static public IEnumerable<(string segment, T data)> SplitTextByDisdjointRanges<T>(
			string text, IEnumerable<(int begin, int end, T data)> ranges)
		{
			int lastRangeEnd = 0;
			foreach (var r in ranges)
			{
				if (r.begin > lastRangeEnd)
					yield return (text.Substring(lastRangeEnd, r.begin - lastRangeEnd), default(T));
				yield return (text.Substring(r.begin, r.end - r.begin), r.data);
				lastRangeEnd = r.end;
			}
			if (lastRangeEnd < text.Length)
				yield return (text.Substring(lastRangeEnd, text.Length - lastRangeEnd), default(T));
		}

		static public IEnumerable<(string text, string linkUrl)> ExtractLinks(
			string text)
		{
			int lastSegmentEnd = 0;
			foreach (Match m in Regex.Matches(text, "<a\\s+href=\\\"(?<href>[^\\\"]*)\\\">(?<text>[^<]*)<\\/a>"))
			{
				if (m.Index > lastSegmentEnd)
					yield return (text.Substring(lastSegmentEnd, m.Index - lastSegmentEnd), null);
				yield return (m.Groups["text"].Value, m.Groups["href"].Value);
				lastSegmentEnd = m.Index + m.Length;
			}
			if (text.Length > lastSegmentEnd)
				yield return (text.Substring(lastSegmentEnd), null);
		}
	}
}
