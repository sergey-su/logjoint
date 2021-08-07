using System.Collections.Generic;

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
    }
}
