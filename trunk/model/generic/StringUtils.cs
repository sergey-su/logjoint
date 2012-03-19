using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public class StringUtils
	{
		public static string TrimInsignificantSpace(string str)
		{
			return str.Trim(InsignificantSpaces);
		}

		public static bool IsValidCSharpIdentifier(string str)
		{
			return identifierRe.Match(str).Success;
		}

		static readonly char[] InsignificantSpaces = new char[] { '\t', '\n', '\r', ' ' };
		static readonly Regex identifierRe = new Regex(@"^\w+$");
		static char[] newLineChars = new char[] { '\r', '\n' };

		public static int GetFirstLineLength(StringSlice s)
		{
			return s.IndexOfAny(newLineChars);
		}

		public static bool IsWordChar(char c)
		{
			return char.IsLetterOrDigit(c) || c == '_';
		}

		static readonly string[] bytesUnits = new string[] { "B", "KB", "MB", "GB", "TB" };

		public static void FormatBytesUserFriendly(long bytes, StringBuilder outBuffer)
		{
			long divisor = 1;
			int unitIdx = 0;
			int maxUnitIdx = bytesUnits.Length - 1;
			for (; ; )
			{
				if (bytes / divisor < 1024 || unitIdx == maxUnitIdx)
				{
					if (divisor == 1)
						outBuffer.Append(bytes);
					else
						outBuffer.AppendFormat("{0:0.0}", (double)bytes / (double)divisor);
					outBuffer.AppendFormat(" {0}", bytesUnits[unitIdx]);
					break;
				}
				else
				{
					divisor *= 1024;
					++unitIdx;
				}
			}
		}

		public static string FormatBytesUserFriendly(long bytes)
		{
			var buf = new StringBuilder();
			FormatBytesUserFriendly(bytes, buf);
			return buf.ToString();
		}

		public static string NormalizeLinebreakes(string text)
		{
			var ret = new StringBuilder(text.Length);
			char prev = '\0';
			foreach (char c in text)
			{
				if (c == '\n' && prev != '\r')
				{
					ret.Append("\r\n");
				}
				else if (prev == '\r' && c != '\n')
				{
					ret.Append('\n');
					ret.Append(c);
				}
				else
				{
					ret.Append(c);
				}
				prev = c;
			}
			if (prev == '\r')
				ret.Append('\n');
			return ret.ToString();
		}

		public struct MultilineText
		{
			StringSlice text;
			bool textIsMultiline;

			public MultilineText(StringSlice txt, bool isMultiline)
			{
				this.text = txt;
				this.textIsMultiline = isMultiline;
			}

			public StringSlice Text { get { return text; } }
			public bool IsMultiline { get { return textIsMultiline; } }

			public int EnumLines(Func<StringSlice, int, bool> callback)
			{
				if (!textIsMultiline)
				{
					if (callback != null)
						callback(text, 0);
					return 1;
				}
				return EnumLines(text, callback);
			}

			public static int EnumLines(StringSlice txt, Func<StringSlice, int, bool> callback)
			{
				int currentIdx = 0;
				bool lastWasR = false;
				int currentStart = 0;
				for (int i = 0; i < txt.Length; ++i)
				{
					bool yieldLine = false;
					int newCurrentStart = currentStart;
					int currentEnd = 0;
					switch (txt[i])
					{
						case '\r':
							if (lastWasR)
							{
								yieldLine = true;
								newCurrentStart = i;
								currentEnd = i - 1;
							}
							lastWasR = true;
							break;
						case '\n':
							yieldLine = true;
							if (lastWasR)
								currentEnd = i - 1;
							else
								currentEnd = i;
							lastWasR = false;
							newCurrentStart = i + 1;
							break;
						default:
							if (lastWasR)
							{
								yieldLine = true;
								newCurrentStart = i;
								currentEnd = i - 1;
							}
							lastWasR = false;
							break;
					}
					if (yieldLine)
					{
						if (callback != null)
							if (!callback(txt.SubString(currentStart, currentEnd - currentStart), currentIdx))
								return currentIdx + 1;
						++currentIdx;
						currentStart = newCurrentStart;
					}
				}
				if (lastWasR)
				{
					if (callback != null)
						if (!callback(txt.SubString(currentStart, txt.Length - currentStart - 1), currentIdx))
							return currentIdx + 1;
					++currentIdx;
				}
				else
				{
					if (callback != null)
						callback(txt.SubString(currentStart, txt.Length - currentStart), currentIdx);
				}
				return currentIdx + 1;
			}

			public StringSlice GetNthTextLine(int lineIdx)
			{
				StringSlice ret = StringSlice.Empty;
				EnumLines(text, (s, idx) =>
				{
					if (idx == lineIdx)
					{
						ret = s;
						return false;
					}
					return true;
				});
				return ret;
			}

			public int GetLinesCount()
			{
				return EnumLines(null);
			}
		}


		public static string GetCSharpStringLiteral(string value)
		{
			var sw = new System.IO.StringWriter();
			csharpProvider.Value.GenerateCodeFromExpression(
				new System.CodeDom.CodePrimitiveExpression(value), sw, new System.CodeDom.Compiler.CodeGeneratorOptions());
			return sw.ToString();
		}

		static Lazy<System.CodeDom.Compiler.CodeDomProvider> csharpProvider;

		static StringUtils()
		{
			csharpProvider = new Lazy<System.CodeDom.Compiler.CodeDomProvider>(
				() => Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#"), true);
		}
	}
}
