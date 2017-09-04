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
			ConfigGeneration.GenerateSimpleLayoutConfig(formatRootElement, layoutString, importLog);
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
			public const string AutoDelimiter = "";
			public string Delimiter = AutoDelimiter;

			public void Load(XmlElement e)
			{
				switch ((e.GetAttribute("delimiter") ?? "").ToLowerInvariant())
				{
					case "auto": Delimiter = AutoDelimiter; break;
					case "comma": Delimiter = ","; break;
					case "custom": Delimiter = (e.GetAttribute("customColumnDelimiter") ?? AutoDelimiter); break;
					case "pipe": Delimiter = new string((char)124, 1); break;
					case "semicolon": Delimiter = ";"; break;
					case "space": Delimiter = " "; break;
					case "tab": Delimiter = "\t"; break;
					default: Delimiter = AutoDelimiter; break;
				}

				switch ((e.GetAttribute("quoting") ?? "").ToLowerInvariant())
				{
					case "all": Quoting = QuotingMode.Always; break;
					case "auto": Quoting = QuotingMode.Auto; break;
					case "nothing": Quoting = QuotingMode.Never; break;
					default: Quoting = QuotingMode.Auto; break;
				}

				QuoteChar = (e.GetAttribute("quoteChar") ?? "").FirstOrDefault('"');

				int columnIdx = 0;
				foreach (var columnElt in e.ChildNodes.OfType<XmlElement>().Where(n => n.Name == "column"))
				{
					var name = columnElt.GetAttribute("name");
					if (string.IsNullOrEmpty(name) || ColumnLayouts.ContainsKey(name))
						name = string.Format("column_{0}", columnIdx + 1);
					var layout = columnElt.GetAttribute("layout");
					if (string.IsNullOrEmpty(layout))
						continue;
					ColumnLayouts.Add(name, layout);
					++columnIdx;
				}
			}
		};

		public static void GenerateRegularGrammarElementForCSVLayout(XmlElement formatRootElement, CsvParams csvParams, ImportLog importLog)
		{
			importLog.Clear();
			ConfigGeneration.GenerateCsvLayoutConfig(formatRootElement, csvParams, importLog);
		}

		public class JsonParams
		{
			public class Layout
			{
				public class Attr
				{
					public Layout JsonLayout;
					public string SimpleLayout;
					public bool Encode = true;
				};
				public Dictionary<string, Attr> Attrs = new Dictionary<string, Attr>();
				public bool SuppressSpaces = false;
			};
			public Layout Root = new Layout();

			public void Load(XmlElement e)
			{
				// todo
				Root.Attrs.Add("", new Layout.Attr());
			}
		};

		public static void GenerateRegularGrammarElementForJsonLayout(XmlElement formatRootElement, JsonParams jsonParams, ImportLog importLog)
		{
			importLog.Clear();
			ConfigGeneration.GenerateJsonLayoutConfig(formatRootElement, jsonParams, importLog);
		}
	}
}
