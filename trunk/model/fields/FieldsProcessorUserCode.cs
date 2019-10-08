using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace LogJoint.FieldsProcessor
{
	public partial class FieldsProcessorImpl
	{
		static class UserCode
		{
			public static string GetProlog(string fieldName)
			{
				return string.Format("/* User code begin. Field: {0} */ ", fieldName);
			}

			public static string GetEpilog(string fieldName)
			{
				return " /* User code end */";
			}

			public struct Entry
			{
				public string UserCode;
				public int Index, Length;
				public string FieldName;
			};

			static readonly Regex userCodeRe = new Regex(@"\/\* User code begin\. Field: (.+?) \*\/ (.+?) \/\* User code end \*\/",
				RegexOptions.Compiled | RegexOptions.Singleline);

			static public IEnumerable<Entry> GetEntries(string code)
			{
				int pos = 0;
				for (; ; )
				{
					Match m = userCodeRe.Match(code, pos);
					if (!m.Success)
						break;
					pos = m.Index + m.Length;

					Entry ret;
					ret.Index = m.Groups[2].Index;
					ret.Length = m.Groups[2].Length;
					ret.FieldName = m.Groups[1].Value;
					ret.UserCode = m.Groups[2].Value;
					yield return ret;

				}
			}

			static readonly Regex newLineRe = new Regex(@"^.*$", RegexOptions.Compiled | RegexOptions.Multiline);

			public struct LineInfo
			{
				public int LineNumber;
				public int Position, Length;
			};

			static public IEnumerable<LineInfo> GetLines(string code)
			{
				int pos = 0;
				for (int lineNum = 1; ; ++lineNum)
				{
					Match m = newLineRe.Match(code, pos);
					if (!m.Success)
						break;

					pos = m.Index + m.Length;

					LineInfo ret;
					ret.Position = m.Index;
					ret.Length = m.Length;
					ret.LineNumber = lineNum;
					yield return ret;

					if (m.Length == 0)
						break;
				}
			}

			public static void FindErrorLocation(string code, Diagnostic error, out int globalErrorPos, out UserCode.Entry? userCodeEntry)
			{
				globalErrorPos = 0;
				userCodeEntry = null;

				if (!error.Location.IsInSource)
					return;
				var lineSpan = error.Location.GetLineSpan();
				if (!lineSpan.IsValid)
					return;

				List<UserCode.LineInfo> lines = new List<UserCode.LineInfo>(UserCode.GetLines(code));

				int lineIdx = lineSpan.EndLinePosition.Line;
				if (lineIdx < 0 || lineIdx >= lines.Count)
					return;

				globalErrorPos = lines[lineIdx].Position + lineSpan.EndLinePosition.Character;
				foreach (UserCode.Entry uce in UserCode.GetEntries(code))
				{
					if (globalErrorPos >= uce.Index && globalErrorPos < (uce.Index + uce.Length))
					{
						userCodeEntry = uce;
						break;
					}
				}
			}
		}
	}
}
