using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.NLog
{
	static class Parser
	{
		public enum TokenType
		{
			Invalid,
			Literal, EscapedLiteral,
			RendererBegin,
			RendererEnd,
			ParamColon, // : in the renderer body
			ParamEq // = in the renderer body
		};
		[DebuggerDisplay("{Type} {Value}")]
		public struct Token
		{
			public TokenType Type;
			public char Value;
			public int Position;
		};

		public static IEnumerable<Token> ParseLayoutString(string str)
		{
			int rendererDepth = 0;
			for (int i = 0; i < str.Length; ++i)
			{
				if (rendererDepth > 0 && str[i] == '\\' && (i + 1) < str.Length)
				{
					++i;
					yield return new Token() { Type = TokenType.EscapedLiteral, Value = str[i], Position = i };
				}
				else if (str[i] == '$' && (i + 1) < str.Length && str[i + 1] == '{')
				{
					++i;
					++rendererDepth;
					yield return new Token() { Type = TokenType.RendererBegin, Value = '{', Position = i - 1 };
				}
				else if (str[i] == '}' && rendererDepth > 0)
				{
					--rendererDepth;
					yield return new Token() { Type = TokenType.RendererEnd, Value = '}', Position = i };
				}
				else if (str[i] == ':' && rendererDepth > 0)
				{
					yield return new Token() { Type = TokenType.ParamColon, Value = ':', Position = i };
				}
				else if (str[i] == '=' && rendererDepth > 0)
				{
					yield return new Token() { Type = TokenType.ParamEq, Value = '=', Position = i };
				}
				else
				{
					yield return new Token() { Type = TokenType.Literal, Value = str[i], Position = i };
				}
			}
		}
	}
}
