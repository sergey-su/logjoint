using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace LogJoint.NLog
{
	public partial class LayoutImporter
	{

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

			public static readonly string NotSpecificRegexp = @"[\S\s]*?";
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
					case "cached":
						return GetInnerLayoutRegexps(renderer, ctx);
					case "filesystem-normalize":
					case "json-encode":
					case "xml-encode":
					case "replace":
					case "rot13":
					case "url-encode":
					case "replace-newlines":
					case "wrapline":
					case "trim-whitespace":
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
							EnumOne(new NodeRegex(
								renderer.Data == "gc" ? @"\d*" : @"\d+", // mono renders empty strings for gc props
								renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "guid":
						return
							EnumOne(new NodeRegex(GetGuidRegex(renderer), renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "logger":
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description,
								NodeRegexFlags.IsNotSpecific, renderer.NodeStart, renderer.NodeEnd))
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
					case "aspnet-request-cookie":
					case "aspnet-request-host":
					case "aspnet-request-method":
					case "aspnet-request-querystring":
					case "aspnet-request-referrer":
					case "aspnet-request-useragent":
					case "aspnet-request-url":
					case "aspnet-session":
					case "aspnet-sessionid":
					case "aspnet-user-authtype":
					case "aspnet-user-identity":
					case "aspnet-user-isauthenticated":
					case "iis-site-name":
					case "asp-request":
					case "asp-session":
					case "basedir":
					case "callsite":
					case "document-uri":
					case "environment":
					case "event-context":
					case "event-properties":
					case "exception":
					case "file-contents":
					case "gdc":
					case "identity":
					case "install-context":
					case "log4jxmlevent":
					case "machinename":
					case "mdc":
					case "mdlc":
					case "message":
					case "ndc":
					case "ndlc":
					case "nlogdir":
					case "performancecounter":
					case "registry":
					case "sl-appinfo":
					case "specialfolder":
					case "stacktrace":
					case "tempdir":
					case "all-event-properties":
					case "assembly-version":
					case "var":
					case "appsetting":
					case "aspnet-mvc-action":
					case "aspnet-mvc-controller":
					case "aspnet-item":
					case "aspnet-traceidentifier":
						return
							EnumOne(new NodeRegex(NotSpecificRegexp, renderer.Description, NodeRegexFlags.IsNotSpecific,
								renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "activityid":
						return
							EnumOne(new NodeRegex(GetGuidRegex('D'), renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "appdomain":
						return
							EnumOne(GetAppDomainNode(renderer))
							.Select(ctx.ApplyContextLimitationsToOutputRegex);
					case "callsite-linenumber":
						return
							EnumOne(new NodeRegex(@"\d+", renderer.Description,
								NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd))
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
				string partRe = @"[\w\-_]+";
				string ret;
				// all is optional to enable parsing logs recorded on mac
				if (userName && domain)
					ret = string.Format(@"({0}([\\\/]{0})?)?", partRe);
				else if (userName || domain)
					ret = string.Format("({0})?", partRe);
				else
					ret = null;
				if (ret != null)
					yield return new NodeRegex(ret, renderer.Description,
						NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd);
			}

			static string GetGpcRegex(Syntax.Node renderer)
			{
				//var norm = GetBoolPropertyDefaultTrue(renderer, "normalize");
				//var difference = GetBoolPropertyDefaultFalse(renderer, "difference");
				//var alignDecimalPoint = GetBoolPropertyDefaultTrue(renderer, "alignDecimalPoint");
				//int precision;
				//if (!int.TryParse(GetNormalizedParamValue(renderer, "precision").FirstOrDefault() ?? "", out precision))
				//	precision = 4;
				//var seconds = GetBoolPropertyDefaultTrue(renderer, "seconds");
				return @"(\d+(\.\d*)?)?"; // optional because it may be missing on mono
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
						return @"([\w\-_]*|\.)";

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

			static NodeRegex GetAppDomainNode(Syntax.Node renderer)
			{
				var format = GetParamValue(renderer, "format").Select(p => p.ToLowerInvariant()).FirstOrDefault();
				if (format == null)
					format = "long";
				if (format == "short")
					return new NodeRegex(@"\d{2}", renderer.Description, NodeRegexFlags.None, renderer.NodeStart, renderer.NodeEnd);
				else if (format == "long")
					return new NodeRegex(@"\d{4}\:" + NotSpecificRegexp, renderer.Description, NodeRegexFlags.IsNotSpecific, renderer.NodeStart, renderer.NodeEnd);
				else
					return new NodeRegex(NotSpecificRegexp, renderer.Description, NodeRegexFlags.IsNotSpecific, renderer.NodeStart, renderer.NodeEnd);
			}

			static string GetGuidRegex(Syntax.Node guidRenderer)
			{
				var format = GetParamValue(guidRenderer, "format").FirstOrDefault();
				if (format == null)
					format = "N";
				if (format == "")
					format = "D";
				return GetGuidRegex(format[0]);
			}

			static string GetGuidRegex(char format)
			{
				switch (format)
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

			static IEnumerable<Syntax.Node> GetInnerLayout(Syntax.Node renderer, string paramName)
			{
				return 
					GetParam(renderer, paramName)
					.Union(GetParam(renderer, ""))
					.SelectMany(param => param.Children.Where(param2 => param2.Type == Syntax.NodeType.Layout)).Take(1);
			}

			static IEnumerable<NodeRegex> GetInnerLayoutRegexps(Syntax.Node renderer, NodeRegexContext ctx,
				string innerLayoutParamName = "inner")
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
				ConvertRendererAmbientPropertiesToNodesHelper(tmp, root, "wrapline", "wrapline");
				foreach (var ambRenderer in tmp)
				{
					ambRenderer.Children.Add(new Syntax.Node(Syntax.NodeType.RendererParam, "inner", "inner", new Syntax.Node(Syntax.NodeType.Layout, "", "", root)));
					root = ambRenderer;
				}
				return root;
			}
		}
	}
}
