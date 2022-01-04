using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using LogJoint.Search;

namespace LogJoint.UI.Presenters.LogViewer
{
	/// <summary>
	/// Encapsulates word boundaries detection logic for selection in the viewer.
	/// Meaning of "word" is extended. Whole guids are treated as words.
	/// </summary>
	internal interface IWordSelection
	{
		Tuple<int, int> FindWordBoundaries(StringSlice line, int pos);
		bool IsWordBoundary(StringSlice line, int b, int e);
		bool IsWord(StringSlice str);
	};

	internal class WordSelection: IWordSelection
	{
		public WordSelection(IRegexFactory regexFactory)
		{
			isPartOfGuidRegex = regexFactory.Create(
				@"^[\da-f\-]{1,36}$", ReOptions.IgnoreCase);
			isGuidRegex = regexFactory.Create(
				@"^[\da-f]{8}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{12}$", ReOptions.IgnoreCase);
			isPartOfIPRegex = regexFactory.Create(
				@"^[\d\.\:]{1,21}$", ReOptions.IgnoreCase);
			ipRegex = regexFactory.Create(
				@"(?<p1>\d{1,3})\.(?<p2>\d{1,3})\.(?<p3>\d{1,3})\.(?<p4>\d{1,3})(\:(?<port>\d+))?", ReOptions.IgnoreCase);
		}

		Tuple<int, int> IWordSelection.FindWordBoundaries(StringSlice line, int pos)
		{
			Func<KeyValuePair<int, char>, bool> isNotAWordChar = c => !StringUtils.IsWordChar(c.Value);

			int begin = line.ZipWithIndex().Take(pos).Reverse().Union(new KeyValuePair<int, char>(-1, ' ')).FirstOrDefault(isNotAWordChar).Key + 1;
			int end = line.ZipWithIndex().Skip(pos).Union(new KeyValuePair<int, char>(line.Length, ' ')).FirstOrDefault(isNotAWordChar).Key;
			if (begin != end)
			{
				if (!TryExpandSelectionToCoverGuid(ref begin, ref end, line))
					TryExpandSelectionToCoverIP(ref begin, ref end, line);
				return new Tuple<int, int>(begin, end);
			}
			return null;
		}

		bool IWordSelection.IsWordBoundary(StringSlice line, int b, int e)
		{
			return e > b && line.IsWordBoundary(b, e);
		}

		bool IWordSelection.IsWord(StringSlice str)
		{
			return 
				str.All(StringUtils.IsWordChar) ||
				isGuidRegex.IsMatch(str) ||
				IsIP(str);
		}

		static bool TryExpandSelection(ref int begin, ref int end, StringSlice line, Func<StringSlice, bool, StringSlice?> testSubstr)
		{
			int begin2 = begin;
			int end2 = end;

			while (begin2 > 0 && testSubstr(line.Slice(begin2 - 1, end2), true) != null)
				--begin2;
			while (end2 <= line.Length - 1 && testSubstr(line.Slice(begin2, end2 + 1), true) != null)
				++end2;

			var finalStr = testSubstr(line.Slice(begin2, end2), false);
			if (finalStr == null)
				return false;

			begin = finalStr.Value.StartIndex - line.StartIndex;
			end = finalStr.Value.EndIndex - line.StartIndex;

			return true;
		}

		bool TryExpandSelectionToCoverGuid(ref int begin, ref int end, StringSlice line)
		{
			return TryExpandSelection(ref begin, ref end, line, (substr, partialTest) => 
				(partialTest ? isPartOfGuidRegex : isGuidRegex).IsMatch(substr) ? substr : new StringSlice?());
		}

		bool TryExpandSelectionToCoverIP(ref int begin, ref int end, StringSlice line)
		{
			return TryExpandSelection(ref begin, ref end, line, (substr, partialTest) =>
			{
				if (partialTest)
					return isPartOfIPRegex.IsMatch(substr) ? substr : new StringSlice?();
				return FindIP(substr);
			});
		}

		bool IsIP(StringSlice str)
		{
			return (FindIP(str) ?? StringSlice.Empty) == str;
		}

		StringSlice? FindIP(StringSlice str)
		{
			IMatch m = null;
			if (!ipRegex.Match(str.Buffer, str.StartIndex, str.Length, ref m))
				return null;
			for (var i = 1; i <= 4; ++i)
				if (int.Parse(m.Groups[i].ToString(str.Buffer)) > 255)
					return null;
			var port = m.Groups[5].ToString(str.Buffer);
			if (port.Length > 0)
				if (int.Parse(port) > 65535)
					return null;
			return m.Groups[0].ToStringSlice(str.Buffer);
		}

		readonly IRegex isPartOfGuidRegex;
		readonly IRegex isGuidRegex;
		readonly IRegex isPartOfIPRegex;
		readonly IRegex ipRegex;
	};
};