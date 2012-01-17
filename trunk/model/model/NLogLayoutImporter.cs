using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace LogJoint.NLog
{
	public class ImportException : Exception
	{
		public ImportException(string msg) : base(msg) { }
	};

	public class ImportLog
	{
		public void AddWarning(string message)
		{

		}
	};

	public class LayoutImporter: IDisposable
	{
		public void Dispose()
		{
		}

		public static void GenerateRegularGrammarElement(XmlElement formatRootElement, string layoutString, ImportLog importLog)
		{
			using (LayoutImporter obj = new LayoutImporter())
				obj.GenerateRegularGrammarElementInternal(formatRootElement, layoutString, importLog);
		}

		void GenerateRegularGrammarElementInternal(XmlElement root, string layoutString, ImportLog log)
		{
			var layout = ParseAndMakeLayoutNode(layoutString);
			var layoutNoAmbProps = SyntaxAnalysis.ConvertAmbientPropertiesToNodes(layout);
			var regexps = SyntaxAnalysis.GetNodeRegexps(layoutNoAmbProps);
			OutputGeneration.GenerateOutput(root, regexps, log);
		}

		static Syntax.Node ParseAndMakeLayoutNode(string layoutString)
		{
			using (var e = Parser.ParseLayoutString(layoutString).GetEnumerator())
				return Syntax.MakeLayoutNode(e, false);
		}

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
			};

			public static IEnumerable<Token> ParseLayoutString(string str)
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
		};

		static class Syntax
		{
			public enum NodeType
			{
				Layout, // Layout sequence. Data not used. Children contains fixed texts and rederers.
				Text, // Fixed text in the layout. Data field contains text string.
				Renderer, // Renderer. Data field containt renderer name. Children contain rederer params.
				RendererParam // Renderer param. Data is a param name. Children has 1 element that stores param value.
			};
			[DebuggerDisplay("{Type} {Data}")]
			public class Node
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

			public static Node MakeLayoutNode(IEnumerator<Parser.Token> toks, bool embeddedLayout)
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
					if (tok.Type == Parser.TokenType.RendererBegin)
					{
						dumpTextNodeIfNotEmpty();
						ret.Children.Add(MakeRendererNode(toks));
					}
					else if (embeddedLayout && (tok.Type == Parser.TokenType.RendererEnd || tok.Type == Parser.TokenType.ParamColon))
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

			static Node MakeRendererNode(IEnumerator<Parser.Token> toks)
			{
				Node ret = new Node(NodeType.Renderer, ReadName(toks));

				while (toks.Current.Type == Parser.TokenType.ParamColon)
				{
					Node param = MakeParamNode(toks);
					if (param != null)
						ret.Children.Add(param);
				}

				return ret;
			}

			static string ReadName(IEnumerator<Parser.Token> toks)
			{
				StringBuilder ret = new StringBuilder();
				for (; ; )
				{
					if (!toks.MoveNext())
						break;
					if (toks.Current.Type == Parser.TokenType.Literal || toks.Current.Type == Parser.TokenType.EscapedLiteral)
						ret.Append(toks.Current.Value);
					else
						break;
				}
				return ret.ToString().ToLower().Trim();
			}

			static Node MakeParamNode(IEnumerator<Parser.Token> toks)
			{
				Node ret = new Node(NodeType.RendererParam, ReadName(toks));
				if (ret.Data.Length == 0)
					return null;
				if (toks.Current.Type != Parser.TokenType.ParamEq)
					return null;
				ret.Children.Add(MakeLayoutNode(toks, true));
				return ret;
			}
		};

		static class SyntaxAnalysis
		{
			[Flags]
			public enum NodeRegexFlags
			{
				None = 0,

				RepresentsDate = 1,
				RepresentsTime = 2,
				RepresentsSeverity = 4,
				RepresentsThread = 8,
				RepresentationMask = RepresentsDate | RepresentsTime | RepresentsSeverity | RepresentsThread,
				RepresentsDateOrTime = RepresentsDate | RepresentsTime,

				IsNotTopLevelRegex = 64,
				IsNotSpecific = 128,
				IsConditional = 256
			};

			public static readonly string NotSpecificRegexp = ".*?";

			public struct NodeRegex
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

			public static IEnumerable<NodeRegex> GetNodeRegexps(Syntax.Node n)
			{
				return GetNodeRegexps(n, new NodeRegexContext());
			}

			static IEnumerable<NodeRegex> GetNodeRegexps(Syntax.Node n, NodeRegexContext ctx)
			{
				switch (n.Type)
				{
					case Syntax.NodeType.Text:
						return
							EnumOne(new NodeRegex(ctx.GetRegexFromStringLiteral(n.Data), "fixed string '" + n.Data + "'"))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case Syntax.NodeType.Layout:
						return n.Children.SelectMany(c => GetNodeRegexps(c, ctx));
					case Syntax.NodeType.Renderer:
						return GetRendererNodeRegexps(n, ctx);
					default:
						return Enumerable.Empty<NodeRegex>();
				}
			}

			static IEnumerable<NodeRegex> GetRendererNodeRegexps(Syntax.Node renderer, NodeRegexContext ctx)
			{
				NodeRegexContext subCtx;
				switch (renderer.Data)
				{
					case "longdate":
						return
							EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}\ \d{2}\:\d{2}\:\d{2}\.\d{4}", "${longdate} yyyy-MM-dd HH:mm:ss.ffff",
								NodeRegexFlags.RepresentsDate | NodeRegexFlags.RepresentsTime) { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ffff" })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "shortdate":
						return
							EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}", "${shortdate} yyyy-MM-dd",
								NodeRegexFlags.RepresentsDate) { DateTimeFormat = "yyyy-MM-dd" })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "time":
						return
							EnumOne(new NodeRegex(@"\d{2}\:\d{2}\:\d{2}\.\d{4}", "${time} HH:mm:ss.ffff",
								NodeRegexFlags.RepresentsTime) { DateTimeFormat = "HH:mm:ss.ffff" })
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
							IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.NotHandleable, "${" + renderer.Data + "}")) : ctx
						);
					case "onexception":
					case "when":
						subCtx = ctx.IncreaseRegexLevel().PushWrapper(
							new RegexModifyingWrapper(WrapperType.Conditional, "${" + renderer.Data + "}"));
						return
							EnumOne(new NodeRegex(@"(", "begin of ${" + renderer.Data + "}"))
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
							EnumOne(new NodeRegex(NotSpecificRegexp, "${" + renderer.Data + "}", NodeRegexFlags.IsNotSpecific))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "level":
						return
							EnumOne(new NodeRegex(
								string.Format("({0}|{1}|{2}|{3})",
									ctx.GetRegexFromStringLiteral("Info"),
									ctx.GetRegexFromStringLiteral("Error"),
									ctx.GetRegexFromStringLiteral("Warn"),
									ctx.GetRegexFromStringLiteral("Debug")),
								"${" + renderer.Data + "}", NodeRegexFlags.RepresentsSeverity))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					default:
						return Enumerable.Empty<NodeRegex>();
				}
			}

			static bool IsRendererEnabled(Syntax.Node renderer)
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

			static bool IsPropertyFalse(Syntax.Node n, string name)
			{
				return GetNormalizedParamValue(n, name).Where(val => val == "false").Any();
			}

			static string GetInnerText(Syntax.Node n)
			{
				switch (n.Type)
				{
					case Syntax.NodeType.Text:
						return n.Data;
					case Syntax.NodeType.Layout:
					case Syntax.NodeType.RendererParam:
						return n.Children.Aggregate(new StringBuilder(), (sb, child) => sb.Append(GetInnerText(child))).ToString();
					default:
						return "";
				}
			}

			static IEnumerable<Syntax.Node> GetParam(Syntax.Node renderer, string paramName)
			{
				return renderer.Children.Where(param => param.Type == Syntax.NodeType.RendererParam && param.Data == paramName).Take(1);
			}

			static IEnumerable<string> GetNormalizedParamValue(Syntax.Node renderer, string paramName)
			{
				return GetParamValue(renderer, paramName).Select(val => val.ToLower().Trim());
			}

			static IEnumerable<string> GetParamValue(Syntax.Node renderer, string paramName)
			{
				return GetParam(renderer, paramName).Select(param => GetInnerText(param));
			}

			static string RendererToString(Syntax.Node renderer)
			{
				var ret = new StringBuilder();
				ret.Append("${");
				ret.Append(renderer.Data);
				renderer.Children.Where(param => param.Type == Syntax.NodeType.RendererParam).Aggregate(ret,
					(s, child) => s.AppendFormat(":{0}={1}", child.Data, GetInnerText(child)));
				ret.Append("}");
				return ret.ToString();
			}

			static IEnumerable<NodeRegex> EnumOne(NodeRegex value)
			{
				yield return value;
			}

			static IEnumerable<Syntax.Node> GetInnerLayout(Syntax.Node renderer, string paramName = "inner")
			{
				return GetParam(renderer, paramName).SelectMany(
					param => param.Children.Where(param2 => param2.Type == Syntax.NodeType.Layout)).Take(1);
			}

			static IEnumerable<NodeRegex> GetInnerLayoutRegexps(Syntax.Node renderer, NodeRegexContext ctx, string innerLayoutParamName = "inner")
			{
				return GetInnerLayout(renderer, innerLayoutParamName).SelectMany(inner => GetNodeRegexps(inner, ctx));
			}

			public static Syntax.Node ConvertAmbientPropertiesToNodes(Syntax.Node root)
			{
				for (int i = 0; i < root.Children.Count; ++i)
					root.Children[i] = ConvertAmbientPropertiesToNodes(root.Children[i]);
				if (root.Type == Syntax.NodeType.Renderer)
					root = ConvertRendererAmbientPropertiesToNodes(root);
				return root;
			}

			static void ConvertRendererAmbientPropertiesToNodesHelper(List<Syntax.Node> foundAmbientRenderers, Syntax.Node renderer, string testedRendererName, params string[] testedRendererParamNames)
			{
				if (renderer.Data != testedRendererName.ToLower())
				{
					var testedRendererParams = new List<Syntax.Node>();
					for (int i = 0; i < renderer.Children.Count; )
					{
						var param = renderer.Children[i];
						if (param.Type == Syntax.NodeType.RendererParam
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
						foundAmbientRenderers.Add(new Syntax.Node(Syntax.NodeType.Renderer, testedRendererName, testedRendererParams.ToArray()));
					}
				}
			}

			static Syntax.Node ConvertRendererAmbientPropertiesToNodes(Syntax.Node root)
			{
				var tmp = new List<Syntax.Node>();
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
					ambRenderer.Children.Add(new Syntax.Node(Syntax.NodeType.RendererParam, "inner", new Syntax.Node(Syntax.NodeType.Layout, "", root)));
					root = ambRenderer;
				}
				return root;
			}
		}

		static class OutputGeneration
		{

			[Flags]
			enum NodeRegexFlag
			{
				None = 0,

				/// <summary>
				/// Node represenets mandatory field (date&time)
				/// </summary>
				Mandatory = 1,
				/// <summary>
				/// Node represenets interesting optional field (thread,severity)
				/// </summary>
				Interesting = 2,
				/// <summary>
				/// Node represents something not interesting for LogJoint
				/// </summary>
				NotInteresting = 4,
				InterestMask = Mandatory | Interesting | NotInteresting,

				/// <summary>
				/// Node's regular expression is specific (i.e. is not generic .*?)
				/// </summary>
				Specific = 8,
				/// <summary>
				/// Node is conditional. Conditional nodes may be omited from the log that makes them less valuable 
				/// for log parser. ${when} is an exampl of renderer that results to conditional node.
				/// </summary>
				Conditional = 16,
				/// <summary>
				/// Node is duplicated by another node that is already included to header regex.
				/// </summary>
				Duplicated = 32,
				/// <summary>
				/// There is not specific node between last captured node and current one
				/// </summary>
				PreceededByNotSpecific = 64
			};

			static int RateReFlags(NodeRegexFlag flags)
			{
				var indexedFlags = flags & (NodeRegexFlag.InterestMask | NodeRegexFlag.Conditional | NodeRegexFlag.Duplicated);
				var ratingsLookupTable = new NodeRegexFlag[] {
					NodeRegexFlag.Mandatory, 
					NodeRegexFlag.Interesting,
					NodeRegexFlag.Mandatory | NodeRegexFlag.Conditional,
					NodeRegexFlag.Interesting | NodeRegexFlag.Conditional,
					NodeRegexFlag.Mandatory | NodeRegexFlag.Duplicated,
					NodeRegexFlag.Interesting | NodeRegexFlag.Duplicated,
					NodeRegexFlag.Mandatory | NodeRegexFlag.Conditional | NodeRegexFlag.Duplicated,
					NodeRegexFlag.Interesting | NodeRegexFlag.Conditional | NodeRegexFlag.Duplicated,
				};
				return ratingsLookupTable.Select((f, i) => new {f,i}).Where(x => x.f==indexedFlags).Select(x => x.i)
					.DefaultIfEmpty(ratingsLookupTable.Length).First();
			}

			public static void GenerateOutput(XmlElement root, IEnumerable<SyntaxAnalysis.NodeRegex> regexpsIt, ImportLog log)
			{
				var regexps = regexpsIt.ToList();

				ValidateRegexpsList(regexps, log);

				var capturableRegexps = CountCapturableRegexps(regexps);

				GenerateOutputInternal(root, regexps.Take(capturableRegexps), log);
			}

			private static int CountCapturableRegexps(List<SyntaxAnalysis.NodeRegex> regexps)
			{
				int capturedRegexps = 0;

				for (int currentReIdx = 0; currentReIdx < regexps.Count; ++currentReIdx)
				{
					var currentReFlags = GetCurrentReFlags(regexps, currentReIdx, capturedRegexps);
					var currentReRating = RateReFlags(currentReFlags);
					var currentReMaxRating = GetMaxRating(currentReFlags);
					if (currentReRating <= currentReMaxRating)
						capturedRegexps = currentReIdx + 1;
				}

				while (capturedRegexps < regexps.Count
				   && (regexps[capturedRegexps].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotTopLevelRegex) != 0)
				{
					++capturedRegexps;
				}

				return capturedRegexps;
			}

			static int GetMaxRating(NodeRegexFlag currentReFlags)
			{
				int maxRating;
				if ((currentReFlags & NodeRegexFlag.PreceededByNotSpecific) != 0)
					maxRating = RateReFlags(NodeRegexFlag.Mandatory | NodeRegexFlag.Duplicated);
				else
					maxRating = RateReFlags(NodeRegexFlag.Interesting | NodeRegexFlag.Conditional | NodeRegexFlag.Duplicated);
				return maxRating;
			}

			static NodeRegexFlag GetCurrentReFlags(List<SyntaxAnalysis.NodeRegex> regexps, int currentReIdx, int capturedRegexps)
			{
				Func<SyntaxAnalysis.NodeRegex, NodeRegexFlag> getInterestFlag = r =>
				{
					if ((r.Flags & (SyntaxAnalysis.NodeRegexFlags.RepresentsDate | SyntaxAnalysis.NodeRegexFlags.RepresentsTime)) != 0)
						return NodeRegexFlag.Mandatory;
					else if ((r.Flags & (SyntaxAnalysis.NodeRegexFlags.RepresentsSeverity | SyntaxAnalysis.NodeRegexFlags.RepresentsThread)) != 0)
						return NodeRegexFlag.Interesting;
					else
						return NodeRegexFlag.NotInteresting;
				};

				Func<SyntaxAnalysis.NodeRegex, NodeRegexFlag> getConditionalFlag = r =>
					(r.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0 ? NodeRegexFlag.Conditional : NodeRegexFlag.None;

				Func<SyntaxAnalysis.NodeRegex, NodeRegexFlag> getSpecificFlag = r =>
					(r.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) == 0 ? NodeRegexFlag.Specific : NodeRegexFlag.None;

				var re = regexps[currentReIdx];

				var reFlags = NodeRegexFlag.None;

				reFlags |= getInterestFlag(re);

				if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) == 0)
					reFlags |= NodeRegexFlag.Specific;

				if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) == 0)
					reFlags |= NodeRegexFlag.Conditional;

				if (regexps.Take(capturedRegexps).Any(
						test => getInterestFlag(test) == getInterestFlag(re) && getConditionalFlag(test) == getConditionalFlag(re)))
					reFlags |= NodeRegexFlag.Duplicated;

				if (regexps.Skip(capturedRegexps).Take(currentReIdx - capturedRegexps).Any(
						test => getSpecificFlag(test) != NodeRegexFlag.Specific))
					reFlags |= NodeRegexFlag.PreceededByNotSpecific;

				return reFlags;
			}

			static void ValidateRegexpsList(List<SyntaxAnalysis.NodeRegex> regexps, ImportLog log)
			{
				if (regexps.Count == 0)
					throw new ImportException("Layout doesn't contain any renderes");

				if ((regexps[0].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0)
					throw new ImportException(string.Format(
						"LogJoint can not match layouts that start from '{0}'. " +
						"Start your layout with a specific renderer like ${{longdate}}.", regexps[0].NodeComment));

				Func<int, bool> isSpecificByIdx = i =>
					i >= 0 && i < regexps.Count && (regexps[i].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) == 0;

				for (int i = 0; i < regexps.Count; ++i)
				{
					var re = regexps[i];
					if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentationMask) != 0
					 && (re.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0)
					{
						bool isSurroundedBySpecificNodes = isSpecificByIdx(i - 1) && isSpecificByIdx(i + 1);
						if (!isSurroundedBySpecificNodes)
						{
							log.AddWarning("Renderer '{0}' can not be matched and as such ignored");
							re.Flags = re.Flags & ~SyntaxAnalysis.NodeRegexFlags.RepresentationMask;
							regexps[i] = re;
						}
					}
				}
			}

			struct CapturedNodeRegex
			{
				public SyntaxAnalysis.NodeRegex Regex;
				public string CaptureName;
			};

			static void GenerateOutputInternal(XmlElement root, IEnumerable<SyntaxAnalysis.NodeRegex> capturedRegexpsIt, ImportLog log)
			{
				var headerReBuilder = new StringBuilder();

				var dateTimeRegexps = new List<CapturedNodeRegex>();
				var threadRegexps = new List<CapturedNodeRegex>();
				var severityRegexps = new List<CapturedNodeRegex>();
				var otherRegexps = new List<CapturedNodeRegex>();
				
				foreach (var re in capturedRegexpsIt)
				{
					List<CapturedNodeRegex> capturesList;
					string capturePrefix;

					if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime) != 0)
					{
						capturesList = dateTimeRegexps;
						capturePrefix = "time";
					}
					else if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentsSeverity) != 0)
					{
						capturesList = severityRegexps;
						capturePrefix = "sev";
					}
					else if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentsThread) != 0)
					{
						capturesList = severityRegexps;
						capturePrefix = "thread";
					}
					else
					{
						capturesList = otherRegexps;
						capturePrefix = "content";
					}

					string captureName = string.Format("{0}{1}", capturePrefix, capturesList.Count + 1);
					capturesList.Add(new CapturedNodeRegex() { Regex = re, CaptureName = captureName });

					if (headerReBuilder.Length > 0)
						headerReBuilder.Append(Environment.NewLine);
					headerReBuilder.AppendFormat("(?<{0}>{1}) # {2}", captureName, re.Regex, re.NodeComment ?? "");
				}

				StringBuilder dateTimeCode = new StringBuilder();

				if (dateTimeRegexps.Count == 0)
					throw new ImportException("Date and time can not be parsed");
				dateTimeCode.AppendFormat("TO_DATETIME({0}, \"{1}\")", dateTimeRegexps[0].CaptureName, dateTimeRegexps[0].Regex.DateTimeFormat);

				StringBuilder bodyCode = new StringBuilder();
				bodyCode.Append("body");


				StringBuilder severityCode = new StringBuilder();
				if (severityRegexps.Count > 0)
				{
					// todo: choose the best severity re, handle optional flag, handle casing, possibly wrapped value
					severityCode.AppendFormat(
@"if ({0}.Length > 0)
switch ({0}[0]) {1}
  case 'E':
  case 'e': 
    return Severity.Error; 
  case 'W':
  case 'w': 
    return Severity.Warning; 
{2}
return Severity.Info;", severityRegexps[0].CaptureName, "{", "}");
				}

				root.AppendChild(root.OwnerDocument.CreateElement("head-re")).AppendChild(root.OwnerDocument.CreateCDataSection(headerReBuilder.ToString()));
				var fieldsNode = root.AppendChild(root.OwnerDocument.CreateElement("fields-config"));

				var timeNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
				timeNode.SetAttribute("name", "Time");
				timeNode.AppendChild(root.OwnerDocument.CreateCDataSection(dateTimeCode.ToString()));
	
				var bodyNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
				bodyNode.SetAttribute("name", "Body");
				bodyNode.AppendChild(root.OwnerDocument.CreateCDataSection(bodyCode.ToString()));

				if (severityCode.Length > 0)
				{
					var severityNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
					severityNode.SetAttribute("name", "Severity");
					severityNode.SetAttribute("code-type", "function");
					severityNode.AppendChild(root.OwnerDocument.CreateCDataSection(severityCode.ToString()));
				}

				// todo: encoding, patterns, search optimization, jitter
			}
		};
	}
}
