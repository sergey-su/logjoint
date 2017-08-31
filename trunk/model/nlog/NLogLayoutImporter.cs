using System.Xml;

namespace LogJoint.NLog
{

	public partial class LayoutImporter
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
				return Syntax.MakeLayoutNode(e, embeddedLayout: false);
		}
	}
}
