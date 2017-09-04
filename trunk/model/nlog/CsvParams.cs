using System.Collections.Generic;
using System.Xml;
using System.Linq;
using LogJoint.Analytics;

namespace LogJoint.NLog
{
	public class CsvParams
	{
		public Dictionary<string, string> ColumnLayouts { get; internal set; } = new Dictionary<string, string>();
		public enum QuotingMode
		{
			Auto,
			Always,
			Never
		};
		public QuotingMode Quoting { get; internal set; } = QuotingMode.Auto;
		public char QuoteChar { get; internal set; } = '"';
		public const string AutoDelimiter = "";
		public string Delimiter { get; internal set; } = AutoDelimiter;
		internal string FalalLoadingError { get; set; }

		internal CsvParams()
		{
		}

		public CsvParams(XmlElement e)
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
				if (string.IsNullOrEmpty(name))
				{
					SetError("Column name is missing in column #" + (columnIdx + 1).ToString());
					continue;
				}
				if (ColumnLayouts.ContainsKey(name))
				{
					SetError("Column name is not unique: " + name);
					continue;
				}
				var layout = columnElt.GetAttribute("layout");
				if (string.IsNullOrEmpty(layout))
				{
					SetError("Layout is empty in column: " + name);
					continue;
				}
				ColumnLayouts.Add(name, layout);
				++columnIdx;
			}
		}

		void SetError(string value)
		{
			if (FalalLoadingError == null)
				FalalLoadingError = value;
		}
	};
}
