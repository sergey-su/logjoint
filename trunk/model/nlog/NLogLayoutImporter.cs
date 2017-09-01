using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace LogJoint.NLog
{

	public partial class LayoutImporter
	{
		// todo: detect encoding, detect if search optimization is possible, jitter, rotation, full date detection (header/footer + rotation)

		public static void GenerateRegularGrammarElementForSimpleLayout(XmlElement formatRootElement, string layoutString, ImportLog importLog)
		{
			importLog.Clear();
			LayoutImporter obj = new LayoutImporter();
			obj.GenerateSimpleLayout(formatRootElement, layoutString, importLog);
		}

		public class CsvParams
		{
			public Dictionary<string, string> ColumnLayouts = new Dictionary<string, string>();
			public enum QuotingMode
			{
				Auto,
				Always,
				Never
			};
			public QuotingMode Quoting = QuotingMode.Auto;
			public char QuoteChar = '"';
			public const char AutoDelimiter = '\0';
			public char Delimiter = AutoDelimiter;
		};

		public static void GenerateRegularGrammarElementForCSVLayout(XmlElement formatRootElement, CsvParams csvParams, ImportLog importLog)
		{
			importLog.Clear();
			LayoutImporter obj = new LayoutImporter();
			obj.GenerateCSVLayout(formatRootElement, csvParams, importLog);
		}

		void GenerateSimpleLayout(XmlElement root, string layoutString, ImportLog log)
		{
			var regexps = ParseLayout(layoutString);
			ConfigGeneration.GenerateSimpleLayoutConfig(root, regexps, log);
		}

		void GenerateCSVLayout(XmlElement root, CsvParams csvParams, ImportLog log)
		{
			var columns = csvParams.ColumnLayouts.ToDictionary(column => column.Key, column => ParseLayout(column.Value));
			ConfigGeneration.GenerateCsvLayoutConfig(root, columns, csvParams, log);
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
	}
}
