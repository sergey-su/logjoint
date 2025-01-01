using System.Xml;

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

        public static void GenerateRegularGrammarElementForCSVLayout(XmlElement formatRootElement, CsvParams csvParams, ImportLog importLog)
        {
            importLog.Clear();
            ConfigGeneration.GenerateCsvLayoutConfig(formatRootElement, csvParams, importLog);
        }

        public static void GenerateRegularGrammarElementForJsonLayout(XmlElement formatRootElement, JsonParams jsonParams, ImportLog importLog)
        {
            importLog.Clear();
            ConfigGeneration.GenerateJsonLayoutConfig(formatRootElement, jsonParams, importLog);
        }
    }
}
