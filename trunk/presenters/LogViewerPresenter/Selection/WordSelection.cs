using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.RegularExpressions;

namespace LogJoint.UI.Presenters.LogViewer
{
	/// <summary>
	/// Encapsulates word boundaries detection logic for selection in the viewer.
	/// Meaning of "word" is extended. For examople, whole guids are treated as words.
	/// </summary>
	internal interface IWordSelection
	{
		Tuple<int, int> FindWordBoundaries(StringSlice line, int pos);
	};

	internal class WordSelection: IWordSelection
	{
		public WordSelection(IRegexFactory regexFactory)
		{
			isPartOfGuidRegex = regexFactory.Create(
				@"^[\da-f\-]{1,36}$", ReOptions.IgnoreCase);
			isGuidRegex = regexFactory.Create(
				@"^[\da-f]{8}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{4}\-[\da-f]{12}$", ReOptions.IgnoreCase);
			isPartOfIPv4Regex = regexFactory.Create(
				@"^[\d\.\:]{1,21}$", ReOptions.IgnoreCase);
			ipIPv4Regex = regexFactory.Create(
				@"(?<p1>\d{1,3})\.(?<p2>\d{1,3})\.(?<p3>\d{1,3})\.(?<p4>\d{1,3})(\:(?<port>\d+))?", ReOptions.IgnoreCase);
			isPartOfIPv6Regex = regexFactory.Create(
				@"^[a-f:\d\[\]]{1,48}$", ReOptions.IgnoreCase);
			ipIPv6WithPortRegex = regexFactory.Create(
				@"\[(?<ip>[a-f:\d]+)\]:(?<port>\d+)", ReOptions.IgnoreCase);
			ipIPv6WithoutPortRegex = regexFactory.Create(
				@"[a-f:\d]+", ReOptions.IgnoreCase);
		}

		Tuple<int, int> IWordSelection.FindWordBoundaries(StringSlice line, int pos)
		{
			static bool isNotAWordChar(KeyValuePair<int, char> c) => !StringUtils.IsWordChar(c.Value);

			int begin = line.ZipWithIndex().Take(pos).Reverse().Union(new KeyValuePair<int, char>(-1, ' ')).FirstOrDefault(isNotAWordChar).Key + 1;
			int end = line.ZipWithIndex().Skip(pos).Union(new KeyValuePair<int, char>(line.Length, ' ')).FirstOrDefault(isNotAWordChar).Key;
			if (begin != end)
			{
				bool expanded = false;
				expanded = expanded || TryExpandSelectionToCoverGuid(ref begin, ref end, line);
				expanded = expanded || TryExpandSelectionToCoverIPv4(ref begin, ref end, line);
				expanded = expanded || TryExpandSelectionToCoverIPv6(ref begin, ref end, line);
				return new Tuple<int, int>(begin, end);
			}
			return null;
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

		bool TryExpandSelectionToCoverIPv4(ref int begin, ref int end, StringSlice line)
		{
			return TryExpandSelection(ref begin, ref end, line, (substr, partialTest) =>
			{
				if (partialTest)
					return isPartOfIPv4Regex.IsMatch(substr) ? substr : new StringSlice?();
				return FindIPv4(substr);
			});
		}

		bool TryExpandSelectionToCoverIPv6(ref int begin, ref int end, StringSlice line)
		{
			return TryExpandSelection(ref begin, ref end, line, (substr, partialTest) =>
			{
				if (partialTest)
					return isPartOfIPv6Regex.IsMatch(substr) ? substr : new StringSlice?();
				return FindIPv6(substr);
			});
		}

		StringSlice? FindIPv4(StringSlice str)
		{
			IMatch m = null;
			if (!ipIPv4Regex.Match(str.Buffer, str.StartIndex, str.Length, ref m))
				return null;
			for (var i = 1; i <= 4; ++i)
				if (int.Parse(m.Groups[i].ToString(str.Buffer)) > 255)
					return null;
			var port = m.Groups[5].ToString(str.Buffer);
			if (port.Length > 0)
				if (!int.TryParse(port, out var portNum) || portNum > 65535)
					return null;
			return m.Groups[0].ToStringSlice(str.Buffer);
		}

		bool IsIPv6WithoutPort(string str)
		{
			var parts = str.Split(new[] { ':' }, StringSplitOptions.None);
			if (parts.Length < 2)
				return false;
			if (parts.Length > 8)
				return false;
			foreach (var part in parts)
			{
				if (part.Length == 0)
					continue;
				if (!uint.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out var partNum) || partNum > 0xffff)
					return false;
			}
			return true;
		}

		StringSlice? FindIPv6(StringSlice str)
		{
			IMatch m = null;
			if (ipIPv6WithPortRegex.Match(str.Buffer, str.StartIndex, str.Length, ref m))
			{
				if (IsIPv6WithoutPort(m.Groups[1].ToString(str.Buffer)) 
				 && int.TryParse(m.Groups[2].ToString(str.Buffer), out var portNum)
				 && portNum < 65538)
					return m.Groups[0].ToStringSlice(str.Buffer);
			}
			m = null;
			if (ipIPv6WithoutPortRegex.Match(str.Buffer, str.StartIndex, str.Length, ref m))
			{
				if (IsIPv6WithoutPort(m.Groups[0].ToString(str.Buffer)))
					return m.Groups[0].ToStringSlice(str.Buffer);
			}
			return null;
		}

		readonly IRegex isPartOfGuidRegex;
		readonly IRegex isGuidRegex;
		readonly IRegex isPartOfIPv4Regex;
		readonly IRegex ipIPv4Regex;
		readonly IRegex isPartOfIPv6Regex;
		readonly IRegex ipIPv6WithPortRegex;
		readonly IRegex ipIPv6WithoutPortRegex;
	};
};