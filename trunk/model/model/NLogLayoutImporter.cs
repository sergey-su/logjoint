using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Globalization;

namespace LogJoint.NLog
{
	public class ImportException : Exception
	{
		public ImportException(string msg) : base(msg) { }
	};

	public class ImportErrorDetectedException : Exception
	{
		public readonly ImportLog Log;
		public ImportErrorDetectedException(string msg, ImportLog log) : base(msg) { Log = log; }
	};

	public class ImportLog
	{
		public enum MessageType
		{
			Invalid = 0,
			NoDateTimeFound,
			DateTimeCannotBeParsed,
			NoTimeParsed,
			NothingToMatch,
			FirstRegexIsNotSpecific,
			ImportantFieldIsConditional,
			RendererUsageReport,
			RendererIgnored,
			UnknownRenderer
		};

		public enum MessageSeverity { Info, Warn, Error };

		public class Message
		{
			public MessageType Type { get; internal set; }
			public MessageSeverity Severity { get; internal set; }
			public class Fragment
			{
				public string Value { get; internal set; }
			};
			public class LayoutSliceLink : Fragment
			{
				public int LayoutSliceStart { get; internal set; }
				public int LayoutSliceEnd { get; internal set; }
			};
			public IEnumerable<Fragment> Fragments { get { return fragments; } }

			public override string ToString()
			{
				return Type.ToString() + ": " + fragments.Select(f => f.Value).Aggregate((ret, frag) => ret + " " + frag);
			}

			internal Message AddText(string txt)
			{
				fragments.Add(new Message.Fragment() { Value = txt });
				return this;
			}

			internal Message AddTextFmt(string fmt, params object[] par)
			{
				return AddText(string.Format(fmt, par));
			}

			internal Message AddLayoutSliceLink(string linkName, int? sliceStart, int? sliceEnd)
			{
				if (sliceStart == null || sliceEnd == null)
					return AddText(linkName);
				fragments.Add(new Message.LayoutSliceLink() { Value = linkName, LayoutSliceStart = sliceStart.Value, LayoutSliceEnd = sliceEnd.Value });
				return this;
			}

			internal Message AddCustom(Action<Message> callback)
			{
				callback(this);
				return this;
			}

			internal List<Fragment> fragments = new List<Fragment>();
		}

		public IEnumerable<Message> Messages { get { return messages; } }

		public bool HasErrors { get { return messages.Any(m => m.Severity == MessageSeverity.Error); } }
		public bool HasWarnings { get { return messages.Any(m => m.Severity == MessageSeverity.Warn); } }

		public void Clear() { messages.Clear(); }

		public override string ToString()
		{
			return messages.Select(m => m.ToString()).Aggregate((ret, s) => ret + " | " + s);
		}

		internal Message AddMessage(MessageType type, MessageSeverity sev)
		{
			var msg = new Message() { Type = type, Severity = sev };
			messages.Add(msg);
			return msg;
		}

		internal void FailIfThereIsError()
		{
			if (messages.Any(m => m.Severity == MessageSeverity.Error))
				throw new ImportErrorDetectedException("Cannot import because of error", this);
		}

		List<Message> messages = new List<Message>();
	};


	public class LayoutImporter
	{
		// todo: detect encoding, detect if search optimization is possible, jitter, rotation, full date detection (header/footer + rotation)

		public static void GenerateRegularGrammarElement(XmlElement formatRootElement, string layoutString, ImportLog importLog)
		{
			importLog.Clear();
			LayoutImporter obj = new LayoutImporter();
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
				public string Description;
				public List<Node> Children;
				public int? NodeStart;
				public int? NodeEnd;
				public Node(NodeType t, string data, string description, params Node[] children)
				{
					Type = t;
					Data = data;
					Description = description;
					Children = new List<Node>(children);
				}
			};

			public static Node MakeLayoutNode(IEnumerator<Parser.Token> toks, bool embeddedLayout)
			{
				Node ret = new Node(NodeType.Layout, "", "");

				var text = new StringBuilder();
				int? textStart = 0;
				int? textEnd = 0;
				Action dumpTextNodeIfNotEmpty = () =>
				{
					if (text.Length > 0)
					{
						ret.Children.Add(new Node(NodeType.Text, text.ToString(), "") { NodeStart = textStart, NodeEnd = textEnd });
						text.Clear();
						textStart = null;
						textEnd = null;
					}
				};

				for (; ; )
				{
					if (!toks.MoveNext())
						break;
					var tok = toks.Current;

					if (ret.NodeStart == null)
						ret.NodeStart = tok.Position;
					ret.NodeEnd = tok.Position;

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
						if (textStart == null)
							textStart = tok.Position;
						textEnd = tok.Position + 1;
					}
				}
				dumpTextNodeIfNotEmpty();

				return ret;
			}

			static Node MakeRendererNode(IEnumerator<Parser.Token> toks)
			{
				Node ret = new Node(NodeType.Renderer, "", "");
				ReadName(ret, toks);
				ret.Description = "${" + ret.Data + "}";

				while (toks.Current.Type == Parser.TokenType.ParamColon)
				{
					Node param = MakeParamNode(toks);
					if (param != null)
						ret.Children.Add(param);
				}

				return ret;
			}

			static bool ReadName(Node destinationNode, IEnumerator<Parser.Token> toks)
			{
				int? start = null;
				int? end = null;
				StringBuilder name = new StringBuilder();
				for (; ; )
				{
					if (!toks.MoveNext())
						break;
					end = toks.Current.Position;
					if (toks.Current.Type == Parser.TokenType.Literal || toks.Current.Type == Parser.TokenType.EscapedLiteral)
						name.Append(toks.Current.Value);
					else
						break;
					if (start == null)
						start = toks.Current.Position;
				}
				destinationNode.Data = name.ToString().ToLower().Trim();
				destinationNode.NodeStart = start;
				destinationNode.NodeEnd = end;
				return destinationNode.Data.Length > 0;
			}

			static Node MakeParamNode(IEnumerator<Parser.Token> toks)
			{
				Node ret = new Node(NodeType.RendererParam, "", "");
				if (!ReadName(ret, toks))
					return null;
				ret.Description = ret.Data;
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
				IsConditional = 256,
				IsIgnorable = 512,
				MakesPreviousCapturable = 1024,
				IsAuxiliaryRegexPart = 2048,
				IsUnknownRenderer = 4096,
				IsStringLiteral = 8192
			};

			public static readonly string NotSpecificRegexp = ".*?";
			public static readonly string TicksFakeDateTimeFormat = "!ticks!";

			public struct NodeRegex
			{
				public string Regex;
				public string NodeDescription;
				public NodeRegexFlags Flags;
				public int? LayoutSliceBegin;
				public int? LayoutSliceEnd;
				public RegexModifyingWrapper? WrapperThatMakesRegexNotSpecific;
				public RegexModifyingWrapper? WrapperThatMakesRegexConditional;
				public string DateTimeFormat;
				public string DateTimeCulture;
				public string StringLiteral;
				public NodeRegex(string re, string description, NodeRegexFlags flags,
					int? layoutSliceBegin, int? layoutSliceEnd)
				{
					if (re == null)
						throw new ArgumentNullException("re");
					Regex = re;
					NodeDescription = description;
					Flags = flags;
					LayoutSliceBegin = layoutSliceBegin;
					LayoutSliceEnd = layoutSliceEnd;
					WrapperThatMakesRegexNotSpecific = null;
					WrapperThatMakesRegexConditional = null;
					DateTimeFormat = null;
					DateTimeCulture = null;
					if (Regex == NotSpecificRegexp)
						Flags |= NodeRegexFlags.IsNotSpecific;
					StringLiteral = null;
				}
				public void AddLinkToSelf(ImportLog.Message msg)
				{
					msg.AddLayoutSliceLink(NodeDescription, LayoutSliceBegin, LayoutSliceEnd);
				}
			};

			public enum WrapperType
			{
				Invalid,
				UpperCase,
				LowerCase,
				NotHandleable,
				Conditional
			};

			public struct RegexModifyingWrapper
			{
				public WrapperType Type;
				public Syntax.Node WrapperRenderer;
				public string CustomRendererName;
				public RegexModifyingWrapper(WrapperType type, Syntax.Node renderer, string customRendererName = null)
				{
					Type = type;
					WrapperRenderer = renderer;
					CustomRendererName = customRendererName;
				}
				public void AddLinkToSelf(ImportLog.Message msg)
				{
					msg.AddLayoutSliceLink(CustomRendererName ?? WrapperRenderer.Description, WrapperRenderer.NodeStart, WrapperRenderer.NodeEnd);
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
						n.WrapperThatMakesRegexNotSpecific = notHandleable;
						n.Flags |= NodeRegexFlags.IsNotSpecific;
					}
					var conditional = wrappersStack.FirstOrDefault(w => w.Type == WrapperType.Conditional);
					if (conditional.Type != WrapperType.Invalid)
					{
						n.WrapperThatMakesRegexConditional = conditional;
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
						wrappersStack = new Stack<RegexModifyingWrapper>(this.wrappersStack),
						regexpLevel = this.regexpLevel
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
							EnumOne(new NodeRegex(ctx.GetRegexFromStringLiteral(n.Data), "fixed string '" + n.Data + "'",
								NodeRegexFlags.IsStringLiteral, n.NodeStart, n.NodeEnd) { StringLiteral = n.Data })
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
							EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}\ \d{2}\:\d{2}\:\d{2}\.\d{4}", renderer.Description,
								NodeRegexFlags.RepresentsDate | NodeRegexFlags.RepresentsTime, renderer.NodeStart, renderer.NodeEnd) { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ffff" })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "shortdate":
						return
							EnumOne(new NodeRegex(@"\d{4}\-\d{2}\-\d{2}", renderer.Description,
								NodeRegexFlags.RepresentsDate, renderer.NodeStart, renderer.NodeEnd) { DateTimeFormat = "yyyy-MM-dd" })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "time":
						return
							EnumOne(new NodeRegex(@"\d{2}\:\d{2}\:\d{2}\.\d{4}", renderer.Description,
								NodeRegexFlags.RepresentsTime, renderer.NodeStart, renderer.NodeEnd) { DateTimeFormat = "HH:mm:ss.ffff" })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "date":
						return
							EnumOne(GetDateNodeRegex(renderer, ctx))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "ticks":
						return
							EnumOne(new NodeRegex(@"\d+", renderer.Description,
								NodeRegexFlags.RepresentsDate | NodeRegexFlags.RepresentsTime, renderer.NodeStart, renderer.NodeEnd) { DateTimeFormat = TicksFakeDateTimeFormat })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "literal":
						return
							GetParamValue(renderer, "text")
							.Select(text => new NodeRegex(ctx.GetRegexFromStringLiteral(text), "literal '" + text + "'",
								NodeRegexFlags.IsStringLiteral, renderer.NodeStart, renderer.NodeEnd) { StringLiteral = text })
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "uppercase":
						return GetInnerLayoutRegexps(
							renderer,
							IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.UpperCase, renderer)) : ctx
						);
					case "lowercase":
						return GetInnerLayoutRegexps(
							renderer,
							IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.LowerCase, renderer)) : ctx
						);
					case "pad":
						return GetPaddingRegexps(renderer, ctx);
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
							IsRendererEnabled(renderer) ? ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.NotHandleable, renderer)) : ctx
						);
					case "onexception":
					case "when":
						subCtx = ctx.IncreaseRegexLevel().PushWrapper(
							new RegexModifyingWrapper(WrapperType.Conditional, renderer));
						return
							EnumOne(new NodeRegex(@"(", "begin of " + renderer.Description, NodeRegexFlags.IsAuxiliaryRegexPart, null, null))
							.Concat(GetInnerLayoutRegexps(renderer, subCtx))
							.Concat(EnumOne(new NodeRegex(@")?", "end of " + renderer.Description, NodeRegexFlags.IsAuxiliaryRegexPart, null, null)))
							.Select(subCtx.ApplyContextLimitationsToOutputRegex);
					case "whenempty":
						subCtx = ctx.IncreaseRegexLevel().PushWrapper(
							new RegexModifyingWrapper(WrapperType.Conditional, renderer));
						return
							EnumOne(new NodeRegex(@"((", "begin of ${whenEmpty}", NodeRegexFlags.IsAuxiliaryRegexPart, null, null))
							.Concat(GetInnerLayoutRegexps(renderer, subCtx, "inner"))
							.Concat(EnumOne(new NodeRegex(@")|(", "OR between alternative layouts of ${whenEmpty}", NodeRegexFlags.IsAuxiliaryRegexPart, null, null)))
							.Concat(GetInnerLayoutRegexps(renderer, subCtx, "whenempty"))
							.Concat(EnumOne(new NodeRegex(@"))", "end of ${whenEmpty}", NodeRegexFlags.IsAuxiliaryRegexPart, null, null)))
							.Select(subCtx.ApplyContextLimitationsToOutputRegex);
					case "level":
						return
							EnumOne(new NodeRegex(
								string.Format("({0}|{1}|{2}|{3}|{4}|{5})",
									ctx.GetRegexFromStringLiteral("Trace"),
									ctx.GetRegexFromStringLiteral("Debug"),
									ctx.GetRegexFromStringLiteral("Info"),
									ctx.GetRegexFromStringLiteral("Warn"),
									ctx.GetRegexFromStringLiteral("Error"),
									ctx.GetRegexFromStringLiteral("Fatal")),
								renderer.Description, 
								NodeRegexFlags.RepresentsSeverity, 
								renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "threadid":
						return
							EnumOne(new NodeRegex(@"\d+", renderer.Description,
								NodeRegexFlags.RepresentsThread, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "threadname":
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description,
								NodeRegexFlags.RepresentsThread | NodeRegexFlags.IsNotSpecific, 
								renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "counter":
					case "gc":
					case "processid":
						return
							EnumOne(new NodeRegex(@"\d+", renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "guid":
						return
							EnumOne(new NodeRegex(GetGuidRegex(renderer), renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "logger":
						return
							EnumOne(new NodeRegex(@"[\w\.\+]+", renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "newline":
						return
							EnumOne(new NodeRegex(@"((\r\n)|\r|\n)", renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "processtime":
						return
							EnumOne(new NodeRegex(@"\d{2}\:\d{2}\:\d{2}\.\d{1,4}", renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "processinfo":
						return
							EnumOne(new NodeRegex(GetProcessInfoRegex(renderer), renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "processname":
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description,
								NodeRegexFlags.IsNotSpecific, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "qpc":
						return
							EnumOne(new NodeRegex(GetGpcRegex(renderer), renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "windows-identity":
						return
							GetWindowsIdentityRegexps(renderer)
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "asp-application":
					case "aspnet-application":
					case "aspnet-request":
					case "aspnet-session":
					case "aspnet-sessionid":
					case "aspnet-user-authtype":
					case "aspnet-user-identity":
					case "asp-request":
					case "asp-session":
					case "basedir":
					case "callsite":
					case "document-uri":
					case "environment":
					case "event-context":
					case "exception":
					case "file-contents":
					case "gdc":
					case "identity":
					case "install-context":
					case "log4jxmlevent":
					case "mdc":
					case "message":
					case "ndc":
					case "nlogdir":
					case "performancecounter":
					case "registry":
					case "sl-appinfo":
					case "specialfolder":
					case "stacktrace":
					case "tempdir":
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description, NodeRegexFlags.IsNotSpecific,
								renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					default:
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description,
								NodeRegexFlags.IsUnknownRenderer | NodeRegexFlags.IsNotSpecific, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);						
				}
			}

			static IEnumerable<NodeRegex> GetWindowsIdentityRegexps(Syntax.Node renderer)
			{
				var userName = GetBoolPropertyDefaultTrue(renderer, "userName");
				var domain = GetBoolPropertyDefaultTrue(renderer, "domain");
				string partRe = @"[a-zA-Z\-_]+";
				string ret;
				if (userName && domain)
					ret = string.Format(@"{0}[\\\/]{0}", partRe);
				else if (userName || domain)
					ret = partRe;
				else
					ret = null;
				if (ret != null)
					yield return new NodeRegex(ret, renderer.Description,
						NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd);
			}

			static string GetGpcRegex(Syntax.Node renderer)
			{
				var norm = GetBoolPropertyDefaultTrue(renderer, "normalize");
				var difference = GetBoolPropertyDefaultFalse(renderer, "difference");
				var alignDecimalPoint = GetBoolPropertyDefaultTrue(renderer, "alignDecimalPoint");
				int precision;
				if (!int.TryParse(GetNormalizedParamValue(renderer, "precision").FirstOrDefault() ?? "", out precision))
					precision = 4;
				var seconds = GetBoolPropertyDefaultTrue(renderer, "seconds");
				return @"\d+(\.\d*)?";
			}

			static string GetProcessInfoRegex(Syntax.Node renderer)
			{
				var prop = GetNormalizedParamValue(renderer, "property").FirstOrDefault();
				if (prop == null)
					prop = "Id";
				switch (prop)
				{
					// positive numbers
					case "BasePriority":
					case "Handle":
					case "HandleCount":
					case "Id":
					case "MainWindowHandle":
					case "MaxWorkingSet":
					case "MinWorkingSet":
					case "NonpagedSystemMemorySize":
					case "NonPagedSystemMemorySize":
					case "NonpagedSystemMemorySize64":
					case "NonPagedSystemMemorySize64":
					case "PagedMemorySize":
					case "PagedMemorySize64":
					case "PagedSystemMemorySize":
					case "PagedSystemMemorySize64":
					case "PeakPagedMemorySize":
					case "PeakPagedMemorySize64":
					case "PeakVirtualMemorySize":
					case "PeakVirtualMemorySize64":
					case "PeakWorkingSet":
					case "PeakWorkingSet64":
					case "PrivateMemorySize":
					case "PrivateMemorySize64":
					case "SessionId":
					case "VirtualMemorySize":
					case "VirtualMemorySize64":
					case "WorkingSet":
					case "WorkingSet64":
						return @"\d*"; // * because I suspect the data is not available on all patforms

					// possibly unavailable integer
					case "ExitCode":
						return @"(\-?\d+)?";

					// bools
					case "HasExited": 
					case "PriorityBoostEnabled":
					case "Responding":
						return "True|False";

					case "MachineName":
						return @"([a-zA-Z\-_]*|\.)";

					case "PriorityClass":
						return @"\w*";

					case "PrivilegedProcessorTime":
					case "TotalProcessorTime":
					case "UserProcessorTime":
						return @"(\d{2}\:\d{2}\:\d{2}\.\d+)?";

					// Not matchable stuff
					case "ExitTime": // it seems this prop doesn't make any sense for running and actively logging process
					case "MainModule":
					case "MainWindowTitle":
					case "ProcessName":
					case "StartTime": // it's not clear what format is used by NLog
						return NotSpecificRegexp;
					default:
						return NotSpecificRegexp;
				}
			}

			static string GetGuidRegex(Syntax.Node guidRenderer)
			{
				var format = GetParamValue(guidRenderer, "format").FirstOrDefault();
				if (format == null)
					format = "N";
				if (format == "")
					format = "D";
				switch (format[0])
				{
					case 'N':
						return @"[\da-fA-F]{32}";
					case 'D':
						return @"[\da-fA-F\-]{36}";
					case 'B':
						return @"\{[\da-fA-F\-]{36}\}";
					case 'P':
						return @"\([\da-fA-F\-]{36}\)";
					case 'X':
						return @"\{0x[x\da-fA-F\,\{]{63}\}\}";
					default:
						return NotSpecificRegexp;
				}
			}

			static CultureInfo GetRendererCulture(Syntax.Node renderer)
			{
				var cultureParamValue = GetParamValue(renderer, "culture").FirstOrDefault();
				if (string.IsNullOrEmpty(cultureParamValue))
					return CultureInfo.InvariantCulture;
				try
				{
					return CultureInfo.GetCultureInfo(cultureParamValue);
				}
				catch (ArgumentException)
				{
					return CultureInfo.InvariantCulture;
				}
			}

			class FormatParsing_RegexBuilderHook : DateTimeFormatParsing.IRegexBuilderHook
			{
				public NodeRegexContext ctx;
				public string GetRegexFromStringLiteral(string str) { return ctx.GetRegexFromStringLiteral(str); }
			};

			private static NodeRegex GetDateNodeRegex(Syntax.Node dateRenderer, NodeRegexContext ctx)
			{
				var format = GetParamValue(dateRenderer, "format").FirstOrDefault();
				if (string.IsNullOrEmpty(format))
					format = "G";
				var dateCulture = GetRendererCulture(dateRenderer);
				var parserFormat = DateTimeFormatParsing.ParseDateTimeFormat(format, dateCulture, new FormatParsing_RegexBuilderHook() { ctx = ctx });
				NodeRegexFlags reFlags = NodeRegexFlags.None;
				var fillDateMask = DateTimeFormatParsing.DateTimeFormatFlag.ContainsYear | DateTimeFormatParsing.DateTimeFormatFlag.ContainsMonth | DateTimeFormatParsing.DateTimeFormatFlag.ContainsDay;
				if ((parserFormat.Flags & fillDateMask) == fillDateMask)
					reFlags |= NodeRegexFlags.RepresentsDate;
				var fillTimeMask = DateTimeFormatParsing.DateTimeFormatFlag.ContainsHour;
				if ((parserFormat.Flags & fillTimeMask) == fillTimeMask)
					reFlags |= NodeRegexFlags.RepresentsTime;
				if ((reFlags & NodeRegexFlags.RepresentsDateOrTime) == 0)
					reFlags |= NodeRegexFlags.IsIgnorable;
				return new NodeRegex(parserFormat.Regex, dateRenderer.Description,
					reFlags, dateRenderer.NodeStart, dateRenderer.NodeEnd) { DateTimeFormat = format, DateTimeCulture = dateCulture.Name };
			}

			private static IEnumerable<NodeRegex> GetPaddingRegexps(Syntax.Node renderer, NodeRegexContext ctx)
			{
				int padding;
				int.TryParse(GetNormalizedParamValue(renderer, "padding").DefaultIfEmpty("0").First(), out padding);
				var padChar = GetParamValue(renderer, "padCharacter").DefaultIfEmpty("").First().DefaultIfEmpty(' ').First();
				var fixedLength = GetNormalizedParamValue(renderer, "fixedLength").DefaultIfEmpty("false").First() == "true";

				if (padding == 0)
					return GetInnerLayoutRegexps(renderer, ctx).Select(ctx.ApplyContextLimitationsToOutputRegex);
				if (fixedLength)
					return GetInnerLayoutRegexps(
						renderer,
						ctx.PushWrapper(new RegexModifyingWrapper(WrapperType.NotHandleable, renderer, "padding with fixedLength=True"))
					);
				var paddingRe = new NodeRegex(
					string.Format("{0}{{0,{1}}}",
						ctx.GetRegexFromStringLiteral(new string(padChar, 1)),
						Math.Abs(padding)
					), 
					renderer.Description,
					NodeRegexFlags.None,
					renderer.NodeStart, 
					renderer.NodeEnd
				);
				if (padding > 0)
					return
						EnumOne(paddingRe)
						.Concat(GetInnerLayoutRegexps(renderer, ctx))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
				else
					return
						GetInnerLayoutRegexps(renderer, ctx)
						.Concat(EnumOne(paddingRe))
						.Select(ctx.ApplyContextLimitationsToOutputRegex);
			}

			static bool IsRendererEnabled(Syntax.Node renderer)
			{
				switch (renderer.Data)
				{
					case "uppercase":
						return GetBoolPropertyDefaultTrue(renderer, "uppercase");
					case "lowercase":
						return GetBoolPropertyDefaultTrue(renderer, "lowercase");
					case "filesystem-normalize":
						return GetBoolPropertyDefaultTrue(renderer, "fSNormalize");
					case "cached":
						return GetBoolPropertyDefaultTrue(renderer, "cached");
					case "json-encode":
						return GetBoolPropertyDefaultTrue(renderer, "jsonEncode");
					case "trim-whitespace":
						return GetBoolPropertyDefaultTrue(renderer, "trimWhiteSpace");
					case "xml-encode":
						return GetBoolPropertyDefaultTrue(renderer, "xmlEncode");
				}
				return true;
			}

			static bool IsPropertyFalse(Syntax.Node n, string name)
			{
				return GetNormalizedParamValue(n, name).Where(val => val == "false").Any();
			}

			static bool IsPropertyTrue(Syntax.Node n, string name)
			{
				return GetNormalizedParamValue(n, name).Where(val => val == "true").Any();
			}

			static bool GetBoolPropertyDefaultFalse(Syntax.Node n, string name)
			{
				return IsPropertyTrue(n, name);
			}

			static bool GetBoolPropertyDefaultTrue(Syntax.Node n, string name)
			{
				return !IsPropertyFalse(n, name);
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
				return renderer.Children.Where(param => param.Type == Syntax.NodeType.RendererParam && param.Data == paramName.Trim().ToLower()).Take(1);
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
						var foundAmbientRenderer = new Syntax.Node(Syntax.NodeType.Renderer, testedRendererName, testedRendererName, testedRendererParams.ToArray());
						var paramWithPosition = testedRendererParams.Where(n => n.NodeEnd != null && n.NodeStart != null).FirstOrDefault();
						if (paramWithPosition != null)
						{
							foundAmbientRenderer.NodeStart = paramWithPosition.NodeStart;
							foundAmbientRenderer.NodeEnd = paramWithPosition.NodeEnd;
							foundAmbientRenderer.Description = paramWithPosition.Description;
						}
						foundAmbientRenderers.Add(foundAmbientRenderer);
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
					ambRenderer.Children.Add(new Syntax.Node(Syntax.NodeType.RendererParam, "inner", "inner", new Syntax.Node(Syntax.NodeType.Layout, "", "", root)));
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

				int capturedRegexps2 = capturedRegexps;
				while ((capturedRegexps2 - 1) < regexps.Count
				   && (regexps[capturedRegexps2 - 1].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotTopLevelRegex) != 0)
				{
					++capturedRegexps2;
				}

				int capturedRegexps3;
				// Check if next uncaptured has to be included as making last captured capturable. It's very simple :)
				if (capturedRegexps < regexps.Count
				   && (regexps[capturedRegexps].Flags & SyntaxAnalysis.NodeRegexFlags.MakesPreviousCapturable) != 0)
				{
					capturedRegexps3 = capturedRegexps + 1;
				}
				else
				{
					capturedRegexps3 = capturedRegexps;
				}

				var ret = Math.Max(capturedRegexps2, capturedRegexps3);

				while (ret < regexps.Count && (regexps[ret].Flags & SyntaxAnalysis.NodeRegexFlags.IsStringLiteral) != 0)
					++ret;

				return ret;
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

				Func<SyntaxAnalysis.NodeRegex, SyntaxAnalysis.NodeRegexFlags> getRepresentationMask = r =>
				{
					return r.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentationMask;
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

				if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0)
					reFlags |= NodeRegexFlag.Conditional;

				if (regexps.Take(capturedRegexps).Any(
						test =>
							getRepresentationMask(test) == getRepresentationMask(re) &&
							getInterestFlag(test) == getInterestFlag(re) &&
							getConditionalFlag(test) == getConditionalFlag(re)))
				{
					reFlags |= NodeRegexFlag.Duplicated;
				}

				if (regexps.Skip(capturedRegexps).Take(currentReIdx - capturedRegexps).Any(
						test => getSpecificFlag(test) != NodeRegexFlag.Specific))
					reFlags |= NodeRegexFlag.PreceededByNotSpecific;

				return reFlags;
			}

			static void ValidateRegexpsList(List<SyntaxAnalysis.NodeRegex> regexps, ImportLog log)
			{
				if (regexps.Count == 0)
				{
					log.AddMessage(ImportLog.MessageType.NothingToMatch, ImportLog.MessageSeverity.Error)
						.AddText("Layout doesn't contain any renderes");
					log.FailIfThereIsError();
				}

				if ((regexps[0].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0)
				{
					log.AddMessage(ImportLog.MessageType.FirstRegexIsNotSpecific, ImportLog.MessageSeverity.Error)
						.AddText("LogJoint can not match layouts that start from")
						.AddCustom(regexps[0].AddLinkToSelf)
						.AddText(". Start your layout with a specific renderer like ${longdate}.");
					log.FailIfThereIsError();
				}

				foreach (var unknown in regexps.Where(re => (re.Flags & SyntaxAnalysis.NodeRegexFlags.IsUnknownRenderer) != 0))
				{
					log.AddMessage(ImportLog.MessageType.UnknownRenderer, ImportLog.MessageSeverity.Warn)
						.AddText("Unknown renderer")
						.AddCustom(unknown.AddLinkToSelf)
						.AddText("ignored");
				}

				Func<int, bool> isSpecificByIdx = i =>
					i >= 0 && i < regexps.Count && (regexps[i].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) == 0;

				Action<int> markAsMakingPreviousCapturable = i => 
				{
					if (i >= 0 && i < regexps.Count)
					{
						var tmp = regexps[i];
						tmp.Flags |= SyntaxAnalysis.NodeRegexFlags.MakesPreviousCapturable;
						regexps[i] = tmp;
					}
				};

				for (int i = 0; i < regexps.Count; ++i)
				{
					var re = regexps[i];
					if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentationMask) != 0
					 && (re.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0)
					{
						bool isPreceededBySpecific = isSpecificByIdx(i - 1);
						bool isFollowedBySpecific = isSpecificByIdx(i + 1);
						bool isSurroundedBySpecificNodes = isPreceededBySpecific && isFollowedBySpecific;
						bool isNotSurroundedBySpecificNodes = !isSurroundedBySpecificNodes;
						bool representsFieldThatCanBeNotSpecific =
							(re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentationMask) == SyntaxAnalysis.NodeRegexFlags.RepresentsThread;
						bool representsFieldThatMustBeSpecific = !representsFieldThatCanBeNotSpecific;
						if (isNotSurroundedBySpecificNodes || representsFieldThatMustBeSpecific)
						{
							var warn = log.AddMessage(ImportLog.MessageType.RendererIgnored, ImportLog.MessageSeverity.Warn);
							warn
								.AddText("Renderer")
								.AddCustom(re.AddLinkToSelf)
								.AddText("can not be matched and as such ignored.");
							if (re.WrapperThatMakesRegexNotSpecific != null)
							{
								var wrap = re.WrapperThatMakesRegexNotSpecific.Value;
								warn
									.AddText("Renderer is not matchable because of")
									.AddCustom(wrap.AddLinkToSelf);
							}

							re.Flags = re.Flags & ~SyntaxAnalysis.NodeRegexFlags.RepresentationMask;
							regexps[i] = re;
						}
						if (representsFieldThatCanBeNotSpecific && isSurroundedBySpecificNodes)
						{
							markAsMakingPreviousCapturable(i + 1);
						}
					}
				}
			}

			class CapturedNodeRegex
			{
				public SyntaxAnalysis.NodeRegex Regex;
				public string CaptureName;
			};

			static XmlNode EnsureElement(XmlNode parent, string name)
			{
				var n = parent.SelectSingleNode(name);
				if (n == null)
					n = parent.AppendChild(parent.OwnerDocument.CreateElement(name));
				return n;
			}

			static XmlNode EnsureEmptyElement(XmlNode parent, string name)
			{
				var ret = EnsureElement(parent, name);
				ret.RemoveAll();
				return ret;
			}

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

					if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.IsIgnorable) != 0)
					{
						log.AddMessage(ImportLog.MessageType.RendererIgnored, ImportLog.MessageSeverity.Warn).AddText(
							"Renderer").AddCustom(re.AddLinkToSelf).AddText("was ignored");
						capturesList = null;
						capturePrefix = null;
					}
					else if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime) != 0)
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
						capturesList = threadRegexps;
						capturePrefix = "thread";
					}
					else if ((re.Flags & SyntaxAnalysis.NodeRegexFlags.IsAuxiliaryRegexPart) != 0)
					{
						capturesList = null;
						capturePrefix = null;
					}
					else
					{
						capturesList = otherRegexps;
						capturePrefix = "content";
					}

					if (headerReBuilder.Length > 0)
						headerReBuilder.Append(Environment.NewLine);

					if (capturePrefix != null)
					{
						string captureName = string.Format("{0}{1}", capturePrefix, capturesList.Count + 1);
						capturesList.Add(new CapturedNodeRegex() { Regex = re, CaptureName = captureName });
						headerReBuilder.AppendFormat("(?<{0}>{1}) # {2}", captureName, re.Regex, EscapeRegexComment(re.NodeDescription) ?? "");
					}
					else
					{
						headerReBuilder.AppendFormat("{0} # {1}", re.Regex, EscapeRegexComment(re.NodeDescription) ?? "");
					}
				}

				string dateTimeCode = GetDateTimeCode(dateTimeRegexps, log);
				string severityCode = GetSeverityCode(severityRegexps, log);
				string threadCode = GetThreadCode(threadRegexps, log);
				string bodyCode = GetBodyCode(otherRegexps, log);

				var regGrammar = EnsureElement(root, "regular-grammar");

				EnsureEmptyElement(regGrammar, "head-re").AppendChild(root.OwnerDocument.CreateCDataSection(headerReBuilder.ToString()));
				var fieldsNode = EnsureEmptyElement(regGrammar, "fields-config");

				var timeNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
				timeNode.SetAttribute("name", "Time");
				timeNode.AppendChild(root.OwnerDocument.CreateCDataSection(dateTimeCode));
	
				if (severityCode.Length > 0)
				{
					var severityNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
					severityNode.SetAttribute("name", "Severity");
					severityNode.SetAttribute("code-type", "function");
					severityNode.AppendChild(root.OwnerDocument.CreateCDataSection(severityCode));
				}

				if (threadCode.Length > 0)
				{
					var threadNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
					threadNode.SetAttribute("name", "Thread");
					threadNode.SetAttribute("code-type", "function");
					threadNode.AppendChild(root.OwnerDocument.CreateCDataSection(threadCode));
				}

				var bodyNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
				bodyNode.SetAttribute("name", "Body");
				bodyNode.AppendChild(root.OwnerDocument.CreateCDataSection(bodyCode));
			}

			static string EscapeRegexComment(string str)
			{
				return str.Replace("\r", " ").Replace("\n", " ");
			}

			static void WarnAboutConditionalRenderer(
				CapturedNodeRegex rendererNodeRe,
				string rendererDescription,  
				ImportLog log)
			{
				var re = rendererNodeRe;
				Debug.Assert((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0);
				var warn = log.AddMessage(ImportLog.MessageType.ImportantFieldIsConditional, ImportLog.MessageSeverity.Warn)
					.AddText(rendererDescription)
					.AddCustom(re.Regex.AddLinkToSelf)
					.AddTextFmt("is not guaranteed to produce output");
				if (re.Regex.WrapperThatMakesRegexConditional != null)
					warn.AddText("because it is wrapped by conditional renderer")
						.AddCustom(re.Regex.WrapperThatMakesRegexConditional.Value.AddLinkToSelf);
			}

			static void ReportRendererUsage(
				CapturedNodeRegex rendererNodeRe,
				string outputFieldDescription,
				ImportLog log)
			{
				var re = rendererNodeRe;
				log.AddMessage(ImportLog.MessageType.RendererUsageReport, ImportLog.MessageSeverity.Info)
					.AddText("Renderer")
					.AddCustom(re.Regex.AddLinkToSelf)
					.AddTextFmt("was used to parse {0}", outputFieldDescription);
			}

			private static string GetDateTimeCode(List<CapturedNodeRegex> dateTimeRegexps, ImportLog log)
			{
				Debug.Assert(dateTimeRegexps.All(re => (re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) == 0),
					"Checked in ValidateRegexpsList()");

				StringBuilder dateTimeCode = new StringBuilder();

				Func<SyntaxAnalysis.NodeRegexFlags, bool, CapturedNodeRegex> findDateTimeRe = (representationMask, isConditional) =>
					dateTimeRegexps.FirstOrDefault(re =>
						(re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime) == representationMask
					 && ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0) == isConditional);

				Func<CapturedNodeRegex, string> getDateTimeExpression = re =>
				{
					if (re.Regex.DateTimeFormat == SyntaxAnalysis.TicksFakeDateTimeFormat)
						return string.Format("TICKS_TO_DATETIME({0})", re.CaptureName);
					string fmt = re.Regex.DateTimeCulture == null ? "TO_DATETIME({0}, {1})" : "TO_DATETIME({0}, {1}, \"{2}\")";
					return string.Format(fmt, re.CaptureName, StringUtils.GetCSharpStringLiteral(re.Regex.DateTimeFormat), re.Regex.DateTimeCulture);
				};

				var concreteDateTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime, false);
				var concreteDate = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDate, false);
				var concreteTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsTime, false);
				var conditionalDateTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime, true);
				var conditionalDate = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDate, true);
				var conditionalTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsTime, true);

				var used = new List<CapturedNodeRegex>();

				if (concreteDateTime != null)
				{
					dateTimeCode.Append(getDateTimeExpression(concreteDateTime));
					used.Add(concreteDateTime);
				}
				else if (concreteDate != null && concreteTime != null)
				{
					dateTimeCode.AppendFormat("DATETIME_FROM_DATE_AND_TIMEOFDAY({0}, {1})",
						getDateTimeExpression(concreteDate), getDateTimeExpression(concreteTime));
					used.Add(concreteDate);
					used.Add(concreteTime);
				}
				else if (concreteDate != null)
				{
					dateTimeCode.Append(getDateTimeExpression(concreteDate));
					log.AddMessage(ImportLog.MessageType.NoTimeParsed, ImportLog.MessageSeverity.Warn).AddText(
						"No time renderer found in the layout string. Messages timestamp will have 1 day precision.");
					used.Add(concreteDate);
				}
				else if (concreteTime != null)
				{
					dateTimeCode.AppendFormat("DATETIME_FROM_TIMEOFDAY({0})", getDateTimeExpression(concreteTime));
					used.Add(concreteTime);
				}
				else if (dateTimeRegexps.Count == 0)
				{
					log.AddMessage(ImportLog.MessageType.NoDateTimeFound, ImportLog.MessageSeverity.Error).AddText(
						"Mandatory matchable date/time renderer not found in the layout string");
				}
				else 
				{
					foreach (var re in dateTimeRegexps)
						if ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0)
							WarnAboutConditionalRenderer(re, "date/time renderer", log);
					log.AddMessage(ImportLog.MessageType.DateTimeCannotBeParsed, ImportLog.MessageSeverity.Error).AddText(
						"Layout string contains date/time renderer(s) but they can not be used to reliably parse mandatory message timestamp");
				}
				log.FailIfThereIsError();

				foreach (var re in used)
				{
					ReportRendererUsage(re, "message timestamp", log);
				}

				return dateTimeCode.ToString();
			}

			private static string GetSeverityCode(List<CapturedNodeRegex> severityRegexps, ImportLog log)
			{
				Func<bool, CapturedNodeRegex[]> findSeverityRes = (isConditional) =>
					severityRegexps.Where(re => ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0) == isConditional).ToArray();

				Func<CapturedNodeRegex, string> getCode = re => string.Format(
@"if ({0}.Length > 0)
switch ({0}[0]) {1}
  case 'E':
  case 'e':
  case 'F':
  case 'f':
    return Severity.Error;
  case 'W':
  case 'w':
    return Severity.Warning;
{2}
", re.CaptureName, "{", "}");

				var concreteSeverities = findSeverityRes(false);
				var conditionalSeverities = findSeverityRes(true);

				StringBuilder severityCode = new StringBuilder();
				if (concreteSeverities.Length > 0)
				{
					severityCode.AppendFormat(
@"{0}
return Severity.Info;", getCode(concreteSeverities[0]));
					ReportRendererUsage(concreteSeverities[0], "severity", log);
				}
				else
				{
					foreach (var re in conditionalSeverities)
					{
						ReportRendererUsage(re, "severity", log);
						severityCode.Append(getCode(re));
					}
					severityCode.Append("return Severity.Info;");
				}

				return severityCode.ToString();
			}

			private static string GetThreadCode(List<CapturedNodeRegex> threadRegexps, ImportLog log)
			{
				Func<bool, CapturedNodeRegex[]> findThreads = (isConditional) =>
					threadRegexps.Where(re => ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0) == isConditional).ToArray();

				Func<CapturedNodeRegex, string> getCode = re => 
					string.Format("if ({1}.Length > 0) return {1};{0}", Environment.NewLine, re.CaptureName);

				var concreteThreads = findThreads(false);
				var conditionalThreads = findThreads(true);


				StringBuilder threadCode = new StringBuilder();
				if (concreteThreads.Length > 0)
				{
					threadCode.AppendFormat("{0}return StringSlice.Empty;", getCode(concreteThreads[0]));
					ReportRendererUsage(concreteThreads[0], "thread", log);
				}
				else
				{
					foreach (var re in conditionalThreads)
					{
						ReportRendererUsage(re, "thread", log);
						threadCode.Append(getCode(re));
					}
					threadCode.Append("return StringSlice.Empty;");
				}

				return threadCode.ToString();
			}

			private static bool IsSkipableBodyRe(CapturedNodeRegex re)
			{
				if ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsStringLiteral) == 0)
					return false;
				var skipableChars = "|-/\\ \t-()[]#\"'";
				Func<char, bool> isNotSkipableChar = c => skipableChars.IndexOf(c) < 0;
				return re.Regex.StringLiteral.Any(isNotSkipableChar) == false;
			}

			private static string GetBodyCode(List<CapturedNodeRegex> otherRegexps, ImportLog log)
			{
				StringBuilder bodyCode = new StringBuilder();
				bodyCode.Append("body");
				foreach (var otherRe in otherRegexps.SkipWhile(IsSkipableBodyRe).Reverse<CapturedNodeRegex>())
				{
					bodyCode.Insert(0, "CONCAT(" + otherRe.CaptureName + ", ");
					bodyCode.Append(")");
				}
				return bodyCode.ToString();
			}
		};
	}
}
