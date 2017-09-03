using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.NLog
{
	public partial class LayoutImporter
	{
		static class ConfigGeneration
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

			public static void GenerateSimpleLayoutConfig(XmlElement root, string layoutString, ImportLog log)
			{
				var regexps = ParseLayout(layoutString);

				ReportNoRenderersCondition(regexps.Count, log);
				ValidateFirstHeaderRegex(regexps, log);
				ReportUnknownRenderers(regexps, log);
				ReportMatchabilityProblems(regexps, log);

				var configBuilder = new ConfigBuilder(log, new EscapingOptions());
				configBuilder.AddLayoutRegexps(regexps.Take(CountCapturableRegexps(regexps)), "");
				configBuilder.GenerateConfig(root);
			}

			public static void GenerateCsvLayoutConfig(
				XmlElement root, 
				CsvParams csvParams,
				ImportLog log
			)
			{
				var columnsRegexps = csvParams.ColumnLayouts.ToDictionary(column => column.Key, column => ParseLayout(column.Value));

				ReportNoRenderersCondition(columnsRegexps.Select(c => c.Value.Count).Sum(), log);

				var escapingOptions = GetCsvEscapingOptions(csvParams);
				var delimiterRegex = GetCsvDelimiterRegex(csvParams);

				var configBuilder = new ConfigBuilder(log, escapingOptions);
				configBuilder.HeaderReBuilder.Append("^");
				int columnIdx = 0;
				foreach (var column in columnsRegexps)
				using (new ScopedGuard(() => log.StartHandlingLayout(column.Key), () => log.StopHandlingLayout()))
				{
					if (columnIdx == 0)
						ValidateFirstHeaderRegex(column.Value, log);
					ReportUnknownRenderers(column.Value, log);
					ReportMatchabilityProblems(column.Value, log);

					if (columnIdx > 0)
					{
						configBuilder.HeaderReBuilder.AppendFormat(
							"{0}{1} # CSV separator", Environment.NewLine, delimiterRegex);
					}

					configBuilder.HeaderReBuilder.Append(escapingOptions.QuoteRegex);

					configBuilder.AddLayoutRegexps(column.Value, column.Key);

					configBuilder.HeaderReBuilder.Append(escapingOptions.QuoteRegex);

					++columnIdx;
				}

				configBuilder.GenerateConfig(root);
			}

			public static void GenerateJsonLayoutConfig(
				XmlElement root,
				JsonParams jsonParams,
				ImportLog log
			)
			{
				// todo: ReportNoRenderersCondition

				var configBuilder = new ConfigBuilder(log, new EscapingOptions() { EscapingFormat = "JSON_UNESCAPE({0})" });
				configBuilder.HeaderReBuilder.Append("^");

				Action<JsonParams.Layout, string> handleLayout = null;
				handleLayout = (layout, layoutId) =>
				{
					string spacesRegex = layout.SuppressSpaces ? "" : "\\s";

					configBuilder.HeaderReBuilder.AppendFormat("{0}{1}{2} # json layout begin", Environment.NewLine, '{', spacesRegex);
					foreach (var attr in layout.Attrs)
					{
						configBuilder.HeaderReBuilder.AppendFormat("{0}( # begin of optional group for attr '{1}'", Environment.NewLine, attr.Key);
						configBuilder.HeaderReBuilder.AppendFormat("{0}(\\,{1})? # comma between attrs", Environment.NewLine, spacesRegex);
						configBuilder.HeaderReBuilder.AppendFormat("{0}\"{1}\":{2} # name of attr '{3}'", Environment.NewLine, Regex.Escape(attr.Key), spacesRegex, attr.Key);
						var attrLayoutId = string.Format("{0}[1]", layoutId.Length > 0 ? "." : "", attr.Key);
						if (attr.Value.SimpleLayout != null)
						{
							using (new ScopedGuard(() => log.StartHandlingLayout(attrLayoutId), () => log.StopHandlingLayout()))
							{
								var regexps = ParseLayout(attr.Value.SimpleLayout);
								ReportUnknownRenderers(regexps, log);
								ReportMatchabilityProblems(regexps, log);

								configBuilder.HeaderReBuilder.AppendFormat("{0}\" # value of '{1}' begins", Environment.NewLine, attr.Key);

								configBuilder.AddLayoutRegexps(regexps, attrLayoutId);

								configBuilder.HeaderReBuilder.AppendFormat("{0}\" # value of '{1}' ends", Environment.NewLine, attr.Key);
							}
						}
						else
						{
							handleLayout(attr.Value.JsonLayout, attrLayoutId);
						}
						configBuilder.HeaderReBuilder.AppendFormat("{0})? # end of optional group for attr '{1}'", Environment.NewLine, attr.Key);
					}
					configBuilder.HeaderReBuilder.AppendFormat("{0}{1}{2} # json layout end", Environment.NewLine, spacesRegex, '}');
				};

				handleLayout(jsonParams.Root, "");

				configBuilder.HeaderReBuilder.AppendFormat("{0}\\s", Environment.NewLine);

				configBuilder.GenerateConfig(root);
			}

			static List<SyntaxAnalysis.NodeRegex> ParseLayout(string layoutString)
			{
				Syntax.Node layout;
				using (var e = Parser.ParseLayoutString(layoutString).GetEnumerator())
					layout = Syntax.MakeLayoutNode(e, embeddedLayout: false);
				var layoutNoAmbProps = SyntaxAnalysis.ConvertAmbientPropertiesToNodes(layout);
				var regexps = SyntaxAnalysis.GetNodeRegexps(layoutNoAmbProps);
				return regexps.ToList();
			}

			private static string GetCsvDelimiterRegex(CsvParams csvParams)
			{
				if (csvParams.Delimiter == CsvParams.AutoDelimiter)
					return @"[\,\;]";
				return Regex.Escape(csvParams.Delimiter);
			}

			private static EscapingOptions GetCsvEscapingOptions(CsvParams csvParams)
			{
				var ret = new EscapingOptions();
				string quoteCharRegex = Regex.Escape(new string(csvParams.QuoteChar, 1));
				var escapeFmt = 
					"CSV_UNESCAPE({0}, '" 
					+ (csvParams.QuoteChar == '\'' ? @"\'" : ("" + csvParams.QuoteChar)) 
					+ "')";
				if (csvParams.Quoting == CsvParams.QuotingMode.Always)
				{
					ret.QuoteRegex = string.Format("{1}{0} # quoting always", quoteCharRegex, Environment.NewLine);
					ret.EscapingFormat = escapeFmt;
				}
				else if (csvParams.Quoting == CsvParams.QuotingMode.Auto)
				{
					ret.QuoteRegex = string.Format("{1}{0}? # possible quoting", quoteCharRegex, Environment.NewLine);
					ret.EscapingFormat = escapeFmt;
				}
				else
				{
					ret.QuoteRegex = "";
					ret.EscapingFormat = "{0}";
				}
				return ret;
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
				   && capturedRegexps2 >= 1
				   && (regexps[capturedRegexps2 - 1].Flags & SyntaxAnalysis.NodeRegexFlags.IsNotTopLevelRegex) != 0)
				{
					++capturedRegexps2;
				}

				int capturedRegexps3;
				// Check if next uncaptured has to be included as making last captured capturable. It's very simple :)
				if (capturedRegexps < regexps.Count
				   && capturedRegexps >= 0
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

			private static void ReportMatchabilityProblems(List<SyntaxAnalysis.NodeRegex> regexps, ImportLog log)
			{
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

			private static void ValidateFirstHeaderRegex(List<SyntaxAnalysis.NodeRegex> regexps, ImportLog log)
			{
				var first = regexps.First();
				if ((first.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0)
				{
					log.AddMessage(ImportLog.MessageType.FirstRegexIsNotSpecific, ImportLog.MessageSeverity.Error)
						.AddText("LogJoint can not match layouts that start from")
						.AddCustom(first.AddLinkToSelf)
						.AddText(". Start your layout with a specific renderer like ${longdate}.");
					log.FailIfThereIsError();
				}
			}

			private static void ReportNoRenderersCondition(int totalRegexpsCount, ImportLog log)
			{
				if (totalRegexpsCount == 0)
				{
					log.AddMessage(ImportLog.MessageType.NothingToMatch, ImportLog.MessageSeverity.Error)
						.AddText("Layout doesn't contain any renderers");
					log.FailIfThereIsError();
				}
			}

			private static void ReportUnknownRenderers(List<SyntaxAnalysis.NodeRegex> regexps, ImportLog log)
			{
				foreach (var unknown in regexps.Where(re => (re.Flags & SyntaxAnalysis.NodeRegexFlags.IsUnknownRenderer) != 0))
				{
					log.AddMessage(ImportLog.MessageType.UnknownRenderer, ImportLog.MessageSeverity.Warn)
						.AddText("Unknown renderer")
						.AddCustom(unknown.AddLinkToSelf)
						.AddText("ignored");
				}
			}

			class CapturedNodeRegex
			{
				public SyntaxAnalysis.NodeRegex Regex;
				public string CaptureName;
				public string LayoutId;
			};

			class EscapingOptions
			{
				public string EscapingFormat = "{0}";
				public string QuoteRegex;
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

			class ConfigBuilder
			{
				readonly ImportLog log;
				readonly EscapingOptions escaping;
				readonly StringBuilder headerReBuilder = new StringBuilder();
				readonly List<CapturedNodeRegex> dateTimeRegexps = new List<CapturedNodeRegex>();
				readonly List<CapturedNodeRegex> threadRegexps = new List<CapturedNodeRegex>();
				readonly List<CapturedNodeRegex> severityRegexps = new List<CapturedNodeRegex>();
				readonly List<CapturedNodeRegex> otherRegexps = new List<CapturedNodeRegex>();

				public ConfigBuilder(ImportLog log, EscapingOptions escaping)
				{
					this.log = log;
					this.escaping = escaping;
				}

				public StringBuilder HeaderReBuilder => headerReBuilder;

				public void AddLayoutRegexps(IEnumerable<SyntaxAnalysis.NodeRegex> regexps, string layoutId)
				{
					foreach (var re in regexps)
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
							capturesList.Add(new CapturedNodeRegex() { Regex = re, CaptureName = captureName, LayoutId = layoutId });
							headerReBuilder.AppendFormat("(?<{0}>{1}) # {2}", captureName, re.Regex, EscapeRegexComment(re.NodeDescription) ?? "");
						}
						else
						{
							headerReBuilder.AppendFormat("{0} # {1}", re.Regex, EscapeRegexComment(re.NodeDescription) ?? "");
						}
					}
				}

				public void GenerateConfig(XmlElement root)
				{
					string dateTimeCode = GetDateTimeCode(dateTimeRegexps, log);
					string severityCode = GetSeverityCode(severityRegexps, log);
					string threadCode = GetThreadCode(threadRegexps, log, escaping);
					string bodyCode = GetBodyCode(otherRegexps, log, escaping);

					var regGrammar = EnsureElement(root, "regular-grammar");

					EnsureEmptyElement(regGrammar, "head-re").ReplaceValueWithCData(headerReBuilder.ToString());
					var fieldsNode = EnsureEmptyElement(regGrammar, "fields-config");

					var timeNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
					timeNode.SetAttribute("name", "Time");
					timeNode.ReplaceValueWithCData(dateTimeCode);

					if (severityCode.Length > 0)
					{
						var severityNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
						severityNode.SetAttribute("name", "Severity");
						severityNode.SetAttribute("code-type", "function");
						severityNode.ReplaceValueWithCData(severityCode);
					}

					if (threadCode.Length > 0)
					{
						var threadNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
						threadNode.SetAttribute("name", "Thread");
						threadNode.SetAttribute("code-type", "function");
						threadNode.ReplaceValueWithCData(threadCode);
					}

					var bodyNode = fieldsNode.AppendChild(root.OwnerDocument.CreateElement("field")) as XmlElement;
					bodyNode.SetAttribute("name", "Body");
					bodyNode.ReplaceValueWithCData(bodyCode);
				}
			};

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
					.AddTextFmt("is not guaranteed to produce output")
					.SetLayoutId(rendererNodeRe.LayoutId);
				if (re.Regex.WrapperThatMakesRegexConditional != null)
					warn.AddText("because it is wrapped by conditional renderer")
						.AddCustom(re.Regex.WrapperThatMakesRegexConditional.Value.AddLinkToSelf)
						.SetLayoutId(rendererNodeRe.LayoutId);
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
					.AddTextFmt("was used to parse {0}", outputFieldDescription)
					.SetLayoutId(rendererNodeRe.LayoutId);
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
				//var conditionalDateTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDateOrTime, true);
				//var conditionalDate = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsDate, true);
				//var conditionalTime = findDateTimeRe(SyntaxAnalysis.NodeRegexFlags.RepresentsTime, true);

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

			private static string GetThreadCode(List<CapturedNodeRegex> threadRegexps, ImportLog log, EscapingOptions escaping)
			{
				Func<bool, CapturedNodeRegex[]> findThreads = (isConditional) =>
					threadRegexps.Where(re => ((re.Regex.Flags & SyntaxAnalysis.NodeRegexFlags.IsConditional) != 0) == isConditional).ToArray();

				Func<CapturedNodeRegex, string> getCode = re => 
					string.Format("if ({2}.Length > 0) return {1};{0}", Environment.NewLine, string.Format(escaping.EscapingFormat, re.CaptureName), re.CaptureName);

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

			private static string GetBodyCode(List<CapturedNodeRegex> otherRegexps, ImportLog log, EscapingOptions escaping)
			{
				StringBuilder bodyCode = new StringBuilder();
				bodyCode.AppendFormat(escaping.EscapingFormat, "body");
				foreach (var otherRe in otherRegexps.SkipWhile(IsSkipableBodyRe).Reverse<CapturedNodeRegex>())
				{
					bodyCode.Insert(0, "CONCAT(" + string.Format(escaping.EscapingFormat, otherRe.CaptureName) + ", ");
					bodyCode.Append(")");
				}
				return bodyCode.ToString();
			}
		};
	}
}
