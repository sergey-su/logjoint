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

		[Flags]
		enum NodeRegexFlag
		{
			None = 0,

			Mandatory = 1, // Node represenets mandatory field (date&time)
			Interesting = 2, // Node interesting not mandatory field (thread,severity)
			NotInteresting = 4, // Node represents something not interesting
			InterestMask = Mandatory | Interesting | NotInteresting,

			NotSpecific = 8, // 
			Conditional = 16,
			Duplicated = 32,
			PreceededByNotSpecific = 64
		};

		void GenerateRegularGrammarElementInternal(XmlElement root, string layoutString)
		{
			Node layout;
			using (var e = ParseLayoutString(layoutString).GetEnumerator())
				layout = MakeLayoutNode(e, false);
			var layoutNoAmbProps = ConvertAmbientPropertiesToNodes(layout);
			var regexps = GetNodeRegexps(layoutNoAmbProps, new NodeRegexContext()).ToArray();

			var ret = regexps.Aggregate(new StringBuilder(), (sb, re) => sb.AppendFormat("{0} # {1}{2}", re.Regex, re.NodeComment ?? "", Environment.NewLine)).ToString();

			var dateTimeRegExps = regexps.Select(re => (re.Flags & (NodeRegexFlags.RepresentsDate | NodeRegexFlags.RepresentsTime)) != 0);

			ret.ToArray();
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
		enum NodeRegexFlags
		{
			None = 0,
			RepresentsDate = 1,
			RepresentsTime = 2,
			RepresentsSeverity = 3,
			RepresentsThread = 4,
			IsNotTopLevelRegex = 8,
			IsNotSpecific = 16,
			IsConditional = 32
		};

		static readonly string NotSpecificRegexp = ".*?";

		struct NodeRegex
		{
			public string Regex;
			public string NodeComment;
			public NodeRegexFlags Flags;
			public string WrapperThatMakesRegexNotSpecific;
			public string WrapperThatMakesRegexConditional;
			public string DateTimeFormat;
			public NodeRegex(string re, string comment = "", NodeRegexFlags flags = NodeRegexFlags.None)
			{
				if (re == null)
					throw new ArgumentNullException("re");
				Regex = re;
				NodeComment = comment;
				Flags = flags;
				WrapperThatMakesRegexNotSpecific = null;
				WrapperThatMakesRegexConditional = null;
				DateTimeFormat = null;
			}
		};

		enum WrapperType
		{
			Invalid,
			UpperCase,
			LowerCase,
			NotHandleable,
			Conditional
		};

		struct RegexModifyingWrapper
		{
			public WrapperType Type;
			public string WrapperName;
			public RegexModifyingWrapper(WrapperType type, string wrapperName)
			{
				Type = type;
				WrapperName = wrapperName;
			}
		};

		class NodeRegexContext
		{
			public NodeRegexContext PushWrapper(RegexModifyingWrapper wrapper)
			{
				var ret = Clone();
				ret.wrappersStack.Push(wrapper);
				return ret;
			}
			public NodeRegexContext IncreaseRegexLevel()
			{
				var ret = Clone();
				ret.regexpLevel++;
				return ret;
			}
			public string GetRegexFromStringLiteral(string s)
			{
				if (!IsHandleable)
					return NotSpecificRegexp;
				var lastCaseWrapper = wrappersStack.LastOrDefault(w => w.Type == WrapperType.LowerCase || w.Type == WrapperType.UpperCase);
				switch (lastCaseWrapper.Type)
				{
					case WrapperType.LowerCase:
						s = s.ToLower();
						break;
					case WrapperType.UpperCase:
						s = s.ToUpper();
						break;
				}
				return Regex.Escape(s);
			}
			public bool IsHandleable
			{
				get { return wrappersStack.All(w => w.Type != WrapperType.NotHandleable); }
			}
			public NodeRegex ApplyContextLimitationsToOutputRegex(NodeRegex n)
			{
				var notHandleable = wrappersStack.FirstOrDefault(w => w.Type == WrapperType.NotHandleable);
				if (notHandleable.Type != WrapperType.Invalid)
				{
					n.Regex = NotSpecificRegexp;
					n.WrapperThatMakesRegexNotSpecific = notHandleable.WrapperName;
					n.Flags |= NodeRegexFlags.IsNotSpecific;
				}
				var conditional = wrappersStack.FirstOrDefault(w => w.Type == WrapperType.Conditional);
				if (conditional.Type != WrapperType.Invalid)
				{
					n.WrapperThatMakesRegexConditional = conditional.WrapperName;
					n.Flags |= NodeRegexFlags.IsConditional;
				}
				if (regexpLevel > 0)
				{
					n.Flags |= NodeRegexFlags.IsNotTopLevelRegex;
				}
				return n;
			}

			public NodeRegexContext()
			{
				wrappersStack = new Stack<RegexModifyingWrapper>();
			}

			NodeRegexContext Clone()
			{
				return new NodeRegexContext()
				{
					wrappersStack = new Stack<RegexModifyingWrapper>(this.wrappersStack)
				};
			}

			Stack<RegexModifyingWrapper> wrappersStack;
			int regexpLevel;
		};

		static IEnumerable<NodeRegex> GetNodeRegexps(Node n, NodeRegexContext ctx)
		{
			switch (n.Type)
			{
				case NodeType.Text:
					return 
						EnumOne(new NodeRegex(ctx.GetRegexFromStringLiteral(n.Data), "fixed string '" + n.Data + "'"))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
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
			NodeRegexContext subCtx;
			switch (renderer.Data)
			{
				case "longdate":
					return
						EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}\ \d{2}\:\d{2}\:\d{2}\.\d{3}", "${longdate} yyyy-MM-dd HH:mm:ss.mmm",
							NodeRegexFlags.RepresentsDate | NodeRegexFlags.RepresentsTime) { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff" })
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "shortdate":
					return
						EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}", "${shortdate} yyyy-MM-dd",
							NodeRegexFlags.RepresentsDate) { DateTimeFormat = "yyyy-MM-dd" })
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "time":
					return
						EnumOne(new NodeRegex(@"\d{2}\:\d{2}\:\d{2}\.\d{3}", "${time} HH:mm:ss.mmm",
							NodeRegexFlags.RepresentsTime) { DateTimeFormat = "HH:mm:ss.fff" })
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				//case "date": // todo
				//    return
				//        EnumOne(new NodeRegex(@"\d{2}\:\d{2}\:\d{2}\.\d{3}", "${time} HH:mm:ss.mmm", NodeRegexFlags.RepresentsTime))
				//        .Select(ctx.ApplyContextLimitationsToOutputRegex);
				//case "ticks": // todo
				case "literal":
					return 
						GetParamValue(renderer, "text")
						.Select(text => new NodeRegex(ctx.GetRegexFromStringLiteral(text), "literal '" + text + "'"))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "uppercase":
					return GetInnerLayoutRegexps(
						renderer, 
						IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.UpperCase, "${uppercase}")) : ctx
					);
				case "lowercase":
					return GetInnerLayoutRegexps(
						renderer, 
						IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.LowerCase, "${lowercase}")) : ctx
					);
				case "pad":
					return
						EnumOne(new NodeRegex(@"\ {0,10}", "padding ")) // todo parse padding params
						.Concat(GetInnerLayoutRegexps(renderer, ctx))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "trim-whitespace":
				case "cached":
					return GetInnerLayoutRegexps(renderer, ctx);
				case "filesystem-normalize":
				case "json-encode":
				case "xml-encode":
				case "replace":
				case "rot13":
				case "url-encode":
					return GetInnerLayoutRegexps(
						renderer, 
						IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.NotHandleable, "${"+renderer.Data+"}")) : ctx
					);
				case "onexception":
				case "when":				
					subCtx = ctx.IncreaseRegexLevel().PushWrapper(
						new RegexModifyingWrapper(WrapperType.Conditional, "${" + renderer.Data + "}"));
					return 
						EnumOne(new NodeRegex(@"(", "begin of ${"+renderer.Data+"}"))
						.Concat(GetInnerLayoutRegexps(renderer, subCtx))
						.Concat(EnumOne(new NodeRegex(@")?", "end of ${" + renderer.Data + "}")))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "whenempty":
					subCtx = ctx.IncreaseRegexLevel().PushWrapper(
						new RegexModifyingWrapper(WrapperType.Conditional, "${" + renderer.Data + "}"));
					return
						EnumOne(new NodeRegex(@"((", "begin of ${whenEmpty}"))
						.Concat(GetInnerLayoutRegexps(renderer, subCtx, "inner"))
						.Concat(EnumOne(new NodeRegex(@")|(", "OR between alternative layouts of ${whenEmpty}")))
						.Concat(GetInnerLayoutRegexps(renderer, subCtx, "whenempty"))
						.Concat(EnumOne(new NodeRegex(@"))", "end of ${whenEmpty}")))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				case "message":
					return
						EnumOne(new NodeRegex(NotSpecificRegexp, "${"+renderer.Data+"}", NodeRegexFlags.IsNotSpecific))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				default:
					return Enumerable.Empty<NodeRegex>();
			}
		}

		static bool IsRendererEnabled(Node renderer)
		{
			switch (renderer.Data)
			{
				case "uppercase":
					return !IsPropertyFalse(renderer, "uppercase");
				case "lowercase":
					return !IsPropertyFalse(renderer, "lowercase");
				case "filesystem-normalize":
					return !IsPropertyFalse(renderer, "fSNormalize");
				case "cached":
					return !IsPropertyFalse(renderer, "cached");
				case "json-encode":
					return !IsPropertyFalse(renderer, "jsonEncode");
				case "trim-whitespace":
					return !IsPropertyFalse(renderer, "trimWhiteSpace");
				case "xml-encode":
					return !IsPropertyFalse(renderer, "xmlEncode");
			}
			return true;
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

		static string RendererToString(Node renderer)
		{
			var ret = new StringBuilder();
			ret.Append("${");
			ret.Append(renderer.Data);
			renderer.Children.Where(param => param.Type == NodeType.RendererParam).Aggregate(ret,
				(s, child) => s.AppendFormat(":{0}={1}", child.Data, GetInnerText(child)));
			ret.Append("}");
			return ret.ToString();
		}

		static IEnumerable<NodeRegex> EnumOne(NodeRegex value)
		{
			yield return value;
		}

		static IEnumerable<Node> GetInnerLayout(Node renderer, string paramName = "inner")
		{
			return GetParam(renderer, paramName).SelectMany(
				param => param.Children.Where(param2 => param2.Type == NodeType.Layout)).Take(1);
		}

		static IEnumerable<NodeRegex> GetInnerLayoutRegexps(Node renderer, NodeRegexContext ctx, string innerLayoutParamName = "inner")
		{
			return GetInnerLayout(renderer, innerLayoutParamName).SelectMany(inner => GetNodeRegexps(inner, ctx));
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
