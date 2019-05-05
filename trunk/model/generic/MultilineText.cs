using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	/// <summary>
	/// Splits input <see cref="StringSlice"/> to text lines and
	/// caches the result to speed up certain queries.
	/// Single-threaded.
	/// </summary>
	public class MultilineText
	{
		readonly StringSlice text;
		List<StringSlice> lines;

		public MultilineText(StringSlice txt)
		{
			this.text = txt;
		}

		/// <summary>
		/// Input string
		/// </summary>
		public StringSlice Text { get { return text; } }

		/// <summary>
		/// Determines if input string has multiple lines. Amortized O(1).
		/// </summary>
		public bool IsMultiline { get { return EnsureLines().Count > 1; } }

		/// <summary>
		/// Returns enumerable sequence of tuples for each text line.
		/// Always contains at least one line. Yields one empty line for empty input string.
		/// </summary>
		public IEnumerable<(StringSlice line, int lineIndex)> Lines
		{
			get
			{
				int lineIdx = 0;
				foreach (var l in EnsureLines())
				{
					yield return (l, lineIdx);
					++lineIdx;
				}
			}
		}

		/// <summary>
		/// Gets line by index. Returns <see cref="StringSlice.Empty"/> if line with given index does not exist. Amortized O(1).
		/// </summary>
		public StringSlice GetNthTextLine(int lineIdx)
		{
			StringSlice ret = EnsureLines().ElementAtOrDefault(lineIdx); 
			return ret.IsInitialized ? ret : StringSlice.Empty;
		}

		/// <summary>
		/// Maps input string char index to line index. O(n), n - number of lines in text
		/// </summary>
		public int? CharIndexToLineIndex(int charIndex)
		{
			foreach (var (line, lineIdx) in Lines)
			{
				var lineBegin = line.StartIndex - text.StartIndex;
				var lineEnd = lineBegin + line.Length;
				if (charIndex >= lineBegin && charIndex <= lineEnd)
					return lineIdx;
			};
			return null;
		}

		/// <summary>
		/// Returns nr of lines in the input text. Amortized O(1).
		/// </summary>
		public int GetLinesCount()
		{
			return EnsureLines().Count;
		}

		private List<StringSlice> EnsureLines()
		{
			if (lines == null)
			{
				lines = text.EnumLines().ToList();
			}
			return lines;
		}

		public override string ToString()
		{
			return Text.ToString();
		}
	}
}
