using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace LogJoint
{
	public class NLogImportException : Exception
	{
		public NLogImportException(string msg) : base(msg) { }
	};

	public class NLogLayoutImporter: IDisposable
	{
		public void Dispose()
		{
		}

		public static void GenerateRegularGrammarElement(XmlElement root, string layoutString)
		{
			using (NLogLayoutImporter obj = new NLogLayoutImporter())
				obj.GenerateRegularGrammarElementInternal(root, layoutString);
		}

		void GenerateRegularGrammarElementInternal(XmlElement root, string layoutString)
		{
			OutputFieldType processedFields = OutputFieldType.None;
			StringBuilder headRe = new StringBuilder();

			Node layout;
			using (var e = ParseLayoutString(layoutString).GetEnumerator())
				layout = MakeLayoutNode(e, false);

			OutputFieldType mandatoryFields = OutputFieldType.DateTime;
			if ((processedFields & mandatoryFields) != mandatoryFields)
				throw new NLogImportException("Date and time were not found in the layout");
		}


		enum TokenType
		{
			Invalid,
			Literal, EscapedLiteral, 
			RendererBegin, 
			RendererEnd,
			ParamColon, // : in the renderer body
			ParamEq // = in the renderer body
		};
		[DebuggerDisplay("{Type} {Value}")]
		struct Token
		{
			public TokenType Type;
			public char Value;
		};

		static IEnumerable<Token> ParseLayoutString(string str)
		{
			int rendererDepth = 0;
			for (int i = 0; i < str.Length; ++i)
			{
				if (str[i] == '\\' && (i + 1) < str.Length)
				{
					++i;
					yield return new Token() { Type = TokenType.EscapedLiteral, Value = str[i] };
				}
				else if (str[i] == '$' && (i + 1) < str.Length && str[i + 1] == '{')
				{
					++i;
					++rendererDepth;
					yield return new Token() { Type = TokenType.RendererBegin, Value = '{' };
				}
				else if (str[i] == '}' && rendererDepth > 0)
				{
					--rendererDepth;
					yield return new Token() { Type = TokenType.RendererEnd, Value = '}' };
				}
				else if (str[i] == ':' && rendererDepth > 0)
				{
					yield return new Token() { Type = TokenType.ParamColon, Value = ':' };
				}
				else if (str[i] == '=' && rendererDepth > 0)
				{
					yield return new Token() { Type = TokenType.ParamEq, Value = '=' };
				}
				else
				{
					yield return new Token() { Type = TokenType.Literal, Value = str[i] };
				}
			}
		}

		enum NodeType
		{
			Layout, // Layout sequence. Data not used. Children contains fixed texts and rederers.
			Text, // Fixed text in the layout. Data field contains text string.
			Renderer, // Renderer. Data field containt renderer name. Children contain rederer params.
			RendererParam // Renderer param. Data is a param name. Children has 1 element that stores param value.
		};
		[DebuggerDisplay("{Type} {Data}")]
		struct Node
		{
			public NodeType Type;
			public string Data;
			public List<Node> Children;
		};

		static Node MakeLayoutNode(IEnumerator<Token> toks, bool embeddedLayout)
		{
			Node ret = new Node() { Type = NodeType.Layout, Children = new List<Node>() };

			var text = new StringBuilder();
			Action dumpTextNodeIfNotEmpty = () =>
			{
				if (text.Length > 0)
				{
					ret.Children.Add(new Node() { Type = NodeType.Text, Data = text.ToString() });
					text.Clear();
				}
			};

			for (; ; )
			{
				if (!toks.MoveNext())
					break;
				var tok = toks.Current;
				if (tok.Type == TokenType.RendererBegin)
				{
					dumpTextNodeIfNotEmpty();
					ret.Children.Add(MakeRendererNode(toks));
				}
				else if (embeddedLayout && (tok.Type == TokenType.RendererEnd || tok.Type == TokenType.ParamColon))
				{
					break;
				}
				else
				{
					text.Append(tok.Value);
				}
			}
			dumpTextNodeIfNotEmpty();

			return ret;
		}

		static Node MakeRendererNode(IEnumerator<Token> toks)
		{
			Node ret = new Node() { Type = NodeType.Renderer, Children = new List<Node>() };

			ret.Data = ReadName(toks);

			while (toks.Current.Type == TokenType.ParamColon)
			{
				Node? param = MakeParamNode(toks);
				if (param != null)
					ret.Children.Add(param.Value);
			}

			return ret;
		}

		static string ReadName(IEnumerator<Token> toks)
		{
			StringBuilder ret = new StringBuilder();
			for (; ; )
			{
				if (!toks.MoveNext())
					break;
				if (toks.Current.Type == TokenType.Literal || toks.Current.Type == TokenType.EscapedLiteral)
					ret.Append(toks.Current.Value);
				else
					break;
			}
			return ret.ToString();
		}

		static Node? MakeParamNode(IEnumerator<Token> toks)
		{
			Node ret = new Node() { Type = NodeType.RendererParam, Children = new List<Node>() };
			ret.Data = ReadName(toks);
			if (ret.Data.Length == 0)
				return null;
			if (toks.Current.Type != TokenType.ParamEq)
				return null;
			ret.Children.Add(MakeLayoutNode(toks, true));
			return ret;
		}

		[Flags]
		enum OutputFieldType
		{
			None = 0,
			DateTime = 1,
			Severity = 2,
			Thread = 3
		};

		//struct ProcessedToken
		//{
		//    public string Regex;
		//    public OutputFieldType Field;
		//};

		//struct ParsedRenderer
		//{
		//    public string Name;
		//    public Dictionary<string, string> Params;
		//};

		//ParsedRenderer ParseRenderer(string rendererString)
		//{
		//    string escapedColonReplacement = "<!!colon!!>";
		//    string escapedEqReplacement = "<!!eq!!>";
		//    string[] splitRenderer = rendererString
		//        .Replace(@"\:", escapedColonReplacement)
		//        .Replace(@"\=", escapedEqReplacement)
		//        .Split(':');
		//    Regex escapeSequenceRe = new Regex(@"\\(.)?");
		//    Func<string, string> unescapeAndNormalize = (s) => 
		//        escapeSequenceRe.Replace(
		//            s.Replace(escapedColonReplacement, @"\:").Replace(escapedEqReplacement, @"\="),
		//            m => m.Groups[1].Value);
		//    ParsedRenderer ret = new ParsedRenderer();
		//    if (splitRenderer.Length == 0)
		//        return ret;
		//    ret.Name = unescapeAndNormalize(splitRenderer[0]);
		//    ret.Params = new Dictionary<string, string>();
		//    foreach (var splitParam in splitRenderer
		//        .Skip(1) // skip renderer name
		//        .Select(p => p.Split('=')) // split param to name->value pairs. pair is represented as array.
		//        .Where(p => p.Length > 0) // skip empty pairs with no name
		//    )
		//        ret.Params[unescapeAndNormalize(splitParam[0])] = unescapeAndNormalize(splitParam.ElementAtOrDefault(1) ?? "");
		//    return ret;
		//}

		//ProcessedToken ProcessToken(Token t)
		//{
		//    if (t.Type == TokenType.Literal)
		//        return new ProcessedToken() { Regex = Regex.Escape(t.Value) };
		//    var parsedRenderer = ParseRenderer(t.Value);
		//    switch (parsedRenderer.Name)
		//    {
		//    }

		//    return new ProcessedToken();
		//}
	}
}
