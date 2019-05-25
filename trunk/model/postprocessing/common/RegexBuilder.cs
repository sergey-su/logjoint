using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogJoint.Postprocessing
{
    public static class RegexBuilder
    {
        public static Regex Create(string template)
        {
            return new Regex(SubstituteTemplate(template), RegexOptions.Compiled | RegexOptions.Multiline);
        }

        static string SubstituteTemplate(string template)
        {
            var evaluator = new MatchEvaluator(ReplaceTemplateItem);
            return TemplateFieldRegex.Replace(template, evaluator);
        }

        static string ReplaceTemplateItem(Match m)
        {
            string fieldName = m.Groups[1].Value;
            string fieldType = m.Groups[2].Value;
            string fieldPattern = FieldType2Pattern[fieldType];
            return "(?<" + fieldName + ">" + fieldPattern + ")";
        }

        private static Regex TemplateFieldRegex = new Regex(@"<(\w+):(\w+)>", RegexOptions.Compiled);
        private static Dictionary<string, string> FieldType2Pattern = new Dictionary<string, string>() {
            {"int", @"[+\-]?\d+"},
            {"sint", @"[+\-]?\d+"},
            {"uint", @"\d+"},
            {"long", @"[+\-]?\d+"},
            {"ulong", @"\d+"},
            {"hex", @"[\da-fA-F]+"},
            {"0hex", @"0x[\da-fA-F]+"},
            {"double", @"[+\-]?\d+(?:\.\d+)?"},
            {"time", @"\d\d:\d\d:\d\d.\d{3}"},
            {"datetime", @"\d{2}-\d{2}-\d{4} \d{2}:\d{2}:\d{2}"},
            {"string", @".*?"},
            {"version4", @"\d+\.\d+\.\d+\.\d+"},
            {"word", @"\w+"},
        };
    }
}
