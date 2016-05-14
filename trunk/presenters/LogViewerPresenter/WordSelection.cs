using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Text.RegularExpressions;

namespace LogJoint.UI.Presenters.LogViewer
{
	/// <summary>
	/// Incapsulates word boundaries detection logic for selection in the viewer.
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
		Tuple<int, int> IWordSelection.FindWordBoundaries(StringSlice line, int pos)
		{
			Func<KeyValuePair<int, char>, bool> isNotAWordChar = c => !StringUtils.IsWordChar(c.Value);

			int begin = line.ZipWithIndex().Take(pos).Reverse().Union(new KeyValuePair<int, char>(-1, ' ')).FirstOrDefault(isNotAWordChar).Key + 1;
			int end = line.ZipWithIndex().Skip(pos).Union(new KeyValuePair<int, char>(line.Length, ' ')).FirstOrDefault(isNotAWordChar).Key;
			if (begin != end)
			{
				TryExpandSelectionToCoverGuid(ref begin, ref end, line);
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
				isGuidRegex.IsMatch(str);
		}

		static bool TryExpandSelectionToCoverGuid(ref int begin, ref int end, StringSlice line)
		{
			Func<int, int, bool, bool> testSubstr = (b, e, partialTest) => 
				(partialTest ? isPartOfGuidRegex : isGuidRegex).IsMatch(line.SubString(b, e - b));

			int begin2 = begin;
			int end2 = end;

			while (begin2 > 0 && testSubstr(begin2 - 1, end2, true))
				--begin2;
			while (end2 <= line.Length - 1 && testSubstr(begin2, end2 + 1, true))
				++end2;

			if (!testSubstr(begin2, end2, false))
				return false;

			begin = begin2;
			end = end2;

			return true;
		}

		static IRegex isPartOfGuidRegex = RegexFactory.Instance.Create(
			@"^[\da-f\-]{1,36}$", ReOptions.IgnoreCase);
		static IRegex isGuidRegex = RegexFactory.Instance.Create(
			@"^[\da-f]{8}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{12}$", ReOptions.IgnoreCase);
	};
};