﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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

		public static string GetCSharpStringLiteral(string value)
		{
			var sw = new System.IO.StringWriter();
			csharpProvider.Value.GenerateCodeFromExpression(
				new System.CodeDom.CodePrimitiveExpression(value), sw, new System.CodeDom.Compiler.CodeGeneratorOptions());
			return sw.ToString();
		}

		public static int FindNextWordInString(string str, int startFrom)
		{
			return WordSearchingStateMachine.FindNextWordInString(str, startFrom);
		}

		public static int FindPrevWordInString(string str, int startFrom)
		{
			return WordSearchingStateMachine.FindPrevWordInString(str, startFrom);
		}

		class WordSearchingStateMachine
		{
			enum State
			{
				Initial, InWord, SkippingWord, Searching, Final
			};
			enum CharType
			{
				Word, Space, Other, EOF
			};
			State state;
			int? result;
			
			static CharType GetCharType(char c)
			{
				if (IsWordChar(c))
					return CharType.Word;
				else if (char.IsWhiteSpace(c))
					return CharType.Space;
				else
					return CharType.Other;
			}
			void SetState(State state, int? result = null)
			{
				this.state = state;
				this.result = result;
			}

			public static int FindPrevWordInString(string str, int startFrom)
			{
				var stateMachine = new WordSearchingStateMachine();
				for (int i = Math.Min(startFrom, str.Length - 1); i >= 0; --i)
					stateMachine.HandleChar_PrevWordMode(GetCharType(str[i]), i);
				stateMachine.HandleChar_PrevWordMode(CharType.EOF, 0);
				return stateMachine.result.GetValueOrDefault(0);
			}

			public static int FindNextWordInString(string str, int startFrom)
			{
				var stateMachine = new WordSearchingStateMachine();
				for (int i = Math.Min(startFrom, str.Length - 1); i < str.Length; ++i)
					stateMachine.HandleChar_NextWordMode(GetCharType(str[i]), i);
				stateMachine.HandleChar_NextWordMode(CharType.EOF, str.Length);
				return stateMachine.result.GetValueOrDefault(str.Length);
			}

			void HandleChar_PrevWordMode(CharType charType, int charIdx)
			{
				switch (state)
				{
					case State.Initial:
						if (charType == CharType.Word)
							SetState(State.InWord);
						else if (charType == CharType.Other || charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
					case State.InWord:
						if (charType == CharType.Word)
							SetState(State.SkippingWord);
						else if (charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.Other || charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
					case State.SkippingWord:
						if (charType == CharType.Word)
							SetState(State.SkippingWord);
						else if (charType == CharType.Other || charType == CharType.Space)
							SetState(State.Final, charIdx + 1);
						else if (charType == CharType.EOF)
							SetState(State.Final, 0);
						break;
					case State.Searching:
						if (charType == CharType.Word)
							SetState(State.SkippingWord);
						else if (charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.Other || charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
				}
			}

			void HandleChar_NextWordMode(CharType charType, int charIdx)
			{
				switch (state)
				{
					case State.Initial:
						if (charType == CharType.Word)
							SetState(State.SkippingWord);
						else if (charType == CharType.Other || charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
					case State.SkippingWord:
						if (charType == CharType.Word)
							SetState(State.SkippingWord);
						else if (charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.Other || charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
					case State.Searching:
						if (charType == CharType.Space)
							SetState(State.Searching);
						else if (charType == CharType.Word || charType == CharType.Other || charType == CharType.EOF)
							SetState(State.Final, charIdx);
						break;
				}
			}

		}

		static Lazy<System.CodeDom.Compiler.CodeDomProvider> csharpProvider;

		static StringUtils()
		{
			csharpProvider = new Lazy<System.CodeDom.Compiler.CodeDomProvider>(
				() => Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#"), true);
		}
	}
}
