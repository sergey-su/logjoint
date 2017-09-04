using System;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.NLog
{
	static partial  class ConfigGeneration
	{
		public static void GenerateJsonLayoutConfig(
			XmlElement root,
			JsonParams jsonParams,
			ImportLog log
		)
		{
			if (jsonParams.FalalLoadingError != null)
			{
				log.AddMessage(ImportLog.MessageType.BadLayout, ImportLog.MessageSeverity.Error).AddText(jsonParams.FalalLoadingError);
				log.FailIfThereIsError();
			}

			var configBuilder = new ConfigBuilder(log, new EscapingOptions() { EscapingFormat = "JSON_UNESCAPE({0})" });
			configBuilder.HeaderReBuilder.Append("^");

			Action<JsonParams.Layout> handleLayout = null;
			handleLayout = (layout) =>
			{
				string spacesRegex = layout.SuppressSpaces ? "" : "\\s";

				configBuilder.HeaderReBuilder.AppendFormat("{0}{1}{2} # json layout begin", Environment.NewLine, '{', spacesRegex);
				foreach (var attr in layout.Attrs)
				{
					configBuilder.HeaderReBuilder.AppendFormat("{0}( # begin of optional group for attr '{1}'", Environment.NewLine, attr.Key);
					configBuilder.HeaderReBuilder.AppendFormat("{0}(\\,{1})? # comma between attrs", Environment.NewLine, spacesRegex);
					configBuilder.HeaderReBuilder.AppendFormat("{0}\"{1}\":{2} # name of attr '{3}'", Environment.NewLine, Regex.Escape(attr.Key), spacesRegex, attr.Key);
					bool attributeCanBeMissing = true;
					if (attr.Value.SimpleLayout != null)
					{
						using (new ScopedGuard(() => log.StartHandlingLayout(attr.Value.Id), () => log.StopHandlingLayout()))
						{
							var regexps = ParseLayout(attr.Value.SimpleLayout);
							ReportUnknownRenderers(regexps, log);
							ReportMatchabilityProblems(regexps, log);

							attributeCanBeMissing = regexps.All(r => (r.Flags & SyntaxAnalysis.NodeRegexFlags.IsNotSpecific) != 0);

							configBuilder.HeaderReBuilder.AppendFormat("{0}\" # value of '{1}' begins", Environment.NewLine, attr.Key);

							configBuilder.AddLayoutRegexps(regexps, attr.Value.Id);

							configBuilder.HeaderReBuilder.AppendFormat(@"{0}(?<!\\)"" # value of '{1}' ends", Environment.NewLine, attr.Key);
						}
					}
					else
					{
						if (attr.Value.Encode)
						{
							log.AddMessage(ImportLog.MessageType.BadLayout, ImportLog.MessageSeverity.Error).AddTextFmt(
								"Attribute '{0}' with nested JSON layout is configured to JSON-encode the output (encode=true). Parsing of such layouts is not supported by LogJoint",
								attr.Key
							);
						}
						attributeCanBeMissing = !attr.Value.JsonLayout.RenderEmptyObject;
						handleLayout(attr.Value.JsonLayout);
					}
					configBuilder.HeaderReBuilder.AppendFormat("{0}){2} # end of group for attr '{1}'", 
						Environment.NewLine, attr.Key, attributeCanBeMissing ? "?" : "");
				}
				if (layout.IncludeAllProperties || layout.IncludeMdc || layout.IncludeMdlc)
				{
					configBuilder.HeaderReBuilder.AppendFormat("{0}(\\,.+?)? # optional extra attributes", Environment.NewLine);
				}
				configBuilder.HeaderReBuilder.AppendFormat("{0}{1}{2} # json layout end", Environment.NewLine, spacesRegex, '}');
			};

			handleLayout(jsonParams.Root);

			configBuilder.GenerateConfig(root);
		}
	}
}
