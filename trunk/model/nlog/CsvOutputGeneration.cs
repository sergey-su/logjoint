using System;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.NLog
{
	static partial class ConfigGeneration
	{
		public static void GenerateCsvLayoutConfig(
			XmlElement root, 
			CsvParams csvParams,
			ImportLog log
		)
		{
			if (csvParams.FalalLoadingError != null)
			{
				log.AddMessage(ImportLog.MessageType.BadLayout, ImportLog.MessageSeverity.Error).AddText(csvParams.FalalLoadingError);
				log.FailIfThereIsError();
			}

			var columnsRegexps = csvParams.ColumnLayouts.ToDictionary(column => column.Key, column => ParseLayout(column.Value));

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
	}
}
