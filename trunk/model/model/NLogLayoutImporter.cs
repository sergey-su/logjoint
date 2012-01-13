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
			var layoutNoAmbProps = ConvertAmbientPropertiesToNodes(layout);
			var res = GetNodeRegexps(layoutNoAmbProps, new NodeRegexContext()).ToArray();

			var ret = res.Aggregate(new StringBuilder(), (sb, re) => sb.AppendFormat("{0} # {1}{2}", re.Regex, re.Comment ?? "", Environment.NewLine)).ToString();

			OutputFieldType mandatoryFields = OutputFieldType.Date | OutputFieldType.Time;
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
		class Node
		{
			public NodeType Type;
			public string Data;
			public List<Node> Children;
			public Node(NodeType t, string data, params Node[] children)
			{
				Type = t;
				Data = data;
				Children = new List<Node>(children);
			}
		};

		static Node MakeLayoutNode(IEnumerator<Token> toks, bool embeddedLayout)
		{
			Node ret = new Node(NodeType.Layout, "");

			var text = new StringBuilder();
			Action dumpTextNodeIfNotEmpty = () =>
			{
				if (text.Length > 0)
				{
					ret.Children.Add(new Node(NodeType.Text, text.ToString()));
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
			Node ret = new Node(NodeType.Renderer, ReadName(toks));

			while (toks.Current.Type == TokenType.ParamColon)
			{
				Node param = MakeParamNode(toks);
				if (param != null)
					ret.Children.Add(param);
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
			return ret.ToString().ToLower().Trim();
		}

		static Node MakeParamNode(IEnumerator<Token> toks)
		{
			Node ret = new Node(NodeType.RendererParam, ReadName(toks));
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
			Date = 1,
			Time = 2,
			Severity = 3,
			Thread = 4
		};

		struct NodeRegex
		{
			public string Regex;
			public string Comment;
			public OutputFieldType Field;
			public NodeRegex(string re, string comment = "", OutputFieldType field = OutputFieldType.None)
			{
				Regex = re;
				Comment = comment;
				Field = field;
			}
		};

		enum StringCaseContext
		{
			None,
			Upper,
			Lower
		};

		struct NodeRegexContext
		{
			public StringCaseContext StringCase;
			public NodeRegexContext SetStringCaseIf(StringCaseContext scase, Func<bool> predicate)
			{
				var ret = this;
				if (predicate())
					ret.StringCase = scase;
				return ret;
			}
			public string ApplyCase(string s)
			{
				switch (StringCase)
				{
					case StringCaseContext.Lower:
						return s.ToLower();
					case StringCaseContext.Upper:
						return s.ToUpper();
					default:
						return s;
				}
			}
		};

		static IEnumerable<NodeRegex> GetNodeRegexps(Node n, NodeRegexContext ctx)
		{
			switch (n.Type)
			{
				case NodeType.Text:
					return EnumOne(new NodeRegex(Regex.Escape(ctx.ApplyCase(n.Data)), "fixed string '"+n.Data+"'" ));
				case NodeType.Layout:
					return n.Children.SelectMany(c => GetNodeRegexps(c, ctx));
				case NodeType.Renderer:
					return GetRendererNodeRegexps(n, ctx);
				default:
					return Enumerable.Empty<NodeRegex>();
			}
		}

		static IEnumerable<NodeRegex> GetRendererNodeRegexps(Node renderer, NodeRegexContext ctx)
		{
			switch (renderer.Data)
			{
				case "shortdate":
					return EnumOne(new NodeRegex(
						@"\d{4}\-\d{2}\-\d{2}", 
						"short date yyyy-MM-dd", 
						OutputFieldType.Date));
				case "literal":
					return GetParamValue(renderer, "text").Select(
						text => new NodeRegex(Regex.Escape(ctx.ApplyCase(text)), "literal '"+text+"'"));
				case "uppercase":
					return GetInnerLayout(renderer).SelectMany(
						inner => GetNodeRegexps(
							inner,
							ctx.SetStringCaseIf(StringCaseContext.Upper, 
								() => !IsPropertyFalse(renderer, "uppercase")
							)
						)
					);
				default:
					return Enumerable.Empty<NodeRegex>();
			}
		}

		static bool IsPropertyFalse(Node n, string name)
		{
			return GetNormalizedParamValue(n, name).Where(val => val == "false").Any();
		}

		static string GetInnerText(Node n)
		{
			switch (n.Type)
			{
				case NodeType.Text:
					return n.Data;
				case NodeType.Layout:
				case NodeType.RendererParam:
					return n.Children.Aggregate(new StringBuilder(), (sb, child) => sb.Append(GetInnerText(child))).ToString();
				default:
					return "";
			}
		}

		static IEnumerable<Node> GetParam(Node renderer, string paramName)
		{
			return renderer.Children.Where(param => param.Type == NodeType.RendererParam && param.Data == paramName).Take(1);
		}

		static IEnumerable<string> GetNormalizedParamValue(Node renderer, string paramName)
		{
			return GetParamValue(renderer, paramName).Select(val => val.ToLower().Trim());
		}

		static IEnumerable<string> GetParamValue(Node renderer, string paramName)
		{
			return GetParam(renderer, paramName).Select(param => GetInnerText(param));
		}

		static IEnumerable<NodeRegex> EnumOne(NodeRegex value)
		{
			yield return value;
		}

		static IEnumerable<Node> GetInnerLayout(Node renderer)
		{
			return GetParam(renderer, "inner").SelectMany(
				param => param.Children.Where(param2 => param2.Type == NodeType.Layout)).Take(1);
		}

		static Node ConvertAmbientPropertiesToNodes(Node root)
		{
			for (int i = 0; i < root.Children.Count; ++i)
				root.Children[i] = ConvertAmbientPropertiesToNodes(root.Children[i]);
			if (root.Type == NodeType.Renderer)
				root = ConvertRendererAmbientPropertiesToNodes(root);
			return root;
		}

		static void ConvertRendererAmbientPropertiesToNodesHelper(List<Node> foundAmbientRenderers, Node renderer, string testedRendererName, params string[] testedRendererParamNames)
		{
			if (renderer.Data != testedRendererName.ToLower())
			{
				var testedRendererParams = new List<Node>();
				for (int i = 0; i < renderer.Children.Count;)
				{
					var param = renderer.Children[i];
					if (param.Type == NodeType.RendererParam 
					 && testedRendererParamNames.Count(s => string.Compare(param.Data, s, true) == 0) > 0)
					{
						testedRendererParams.Add(param);
						renderer.Children.RemoveAt(i);
					}
					else
					{
						++i;
					}
				}
				if (testedRendererParams.Count > 0)
				{
					foundAmbientRenderers.Add(new Node(NodeType.Renderer, testedRendererName, testedRendererParams.ToArray()));
				}
			}
		}

		static Node ConvertRendererAmbientPropertiesToNodes(Node root)
		{
			var tmp = new List<Node>();
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "cached", "cached");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "filesystem-normalize", "fSNormalize");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "json-encode", "jsonEncode");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "lowercase", "lowercase");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "pad", "padCharacter", "padding", "fixedLength");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "trim-whitespace", "trimWhiteSpace");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "uppercase", "uppercase");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "when", "when");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "whenEmpty", "whenEmpty");
			ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "xml-encode", "xmlEncode");
			foreach (var ambRenderer in tmp)
			{
				ambRenderer.Children.Add(new Node(NodeType.RendererParam, "inner", new Node(NodeType.Layout, "", root)));
				root = ambRenderer;
			}
			return root;
		}
	}
}
