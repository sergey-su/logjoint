using System.Xml;
using System.Linq;

namespace LogJoint.NLog
{
    static partial class ConfigGeneration
    {

        public static void GenerateSimpleLayoutConfig(XmlElement root, string layoutString, ImportLog log)
        {
            var regexps = ParseLayout(layoutString);

            ValidateFirstHeaderRegex(regexps, log);
            ReportUnknownRenderers(regexps, log);
            ReportMatchabilityProblems(regexps, log);

            var configBuilder = new ConfigBuilder(log, new EscapingOptions());
            configBuilder.AddLayoutRegexps(regexps.Take(CountCapturableRegexps(regexps)), "");
            configBuilder.GenerateConfig(root);
        }
    }
}
