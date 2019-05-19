using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing.TimeSeries
{
    public class GeneratedRegex
    {
        public enum VType { STR, INT, UINT, FLOAT };

        public static string GetCSharpType(VType vtype)
        {
            if (vtype == VType.INT)
            {
                return "int";
            }
            if (vtype == VType.UINT)
            {
                return "uint";
            }
            if (vtype == VType.FLOAT)
            {
                return "double";
            }
            return "string";
        }

        public class ArgItem
        {
            public string regexPattern;
            public string name;
            public VType vtype;
            public ArgItem(string regexPattern, string name, VType vtype)
            {
                this.regexPattern = regexPattern;
                this.name = name;
                this.vtype = vtype;
            }
        };

        public string GeneratedRegexPattern;
        public List<ArgItem> FoundArgItems;
        public string LoglineName;
        public string SourceArgName;
    };

    public interface IRegexGenerator
    {
        GeneratedRegex GenerateRegexFromFormatString(string formatString);
    };


    public class RegexGenerator: IRegexGenerator
    {
        struct FormatPatternRule
        {
            public string src;
            public string dst;
            public GeneratedRegex.VType vtype;
            public FormatPatternRule(string src, string dst, GeneratedRegex.VType vtype) { this.src = src; this.dst = dst; this.vtype = vtype; }
        };

        static FormatPatternRule[] formatPatternRules = {
            new FormatPatternRule( "d", "\\-?\\d+", GeneratedRegex.VType.INT),
            new FormatPatternRule( "i", "\\-?\\d+", GeneratedRegex.VType.INT),
            new FormatPatternRule("lld", "\\-?\\d+", GeneratedRegex.VType.INT),
            new FormatPatternRule("I64u", "\\-?\\d+", GeneratedRegex.VType.UINT),
            new FormatPatternRule("llu", "\\d+", GeneratedRegex.VType.UINT),
            new FormatPatternRule("ld", "\\-?\\d+", GeneratedRegex.VType.INT),
            new FormatPatternRule("lu", "\\d+", GeneratedRegex.VType.UINT),
            new FormatPatternRule("hu", "\\d+", GeneratedRegex.VType.UINT),
            new FormatPatternRule("u", "\\d+", GeneratedRegex.VType.INT),
            new FormatPatternRule("f", "\\-?[\\.\\d]+(?:[eE]\\+?\\-?\\d+)?", GeneratedRegex.VType.FLOAT),
            new FormatPatternRule("g", "\\-?[\\.\\d]+(?:[eE]\\+?\\-?\\d+)?", GeneratedRegex.VType.FLOAT),
            new FormatPatternRule("x", "[0-9A-Fa-f]+", GeneratedRegex.VType.STR),
            new FormatPatternRule("X", "[0-9A-Fa-f]+", GeneratedRegex.VType.STR),
            new FormatPatternRule("p", "(0x)?[0-9A-Fa-f]+", GeneratedRegex.VType.STR),
            new FormatPatternRule("s", ".*", GeneratedRegex.VType.STR), // TODO: Improve! How about smart logic that detects format from the example line?
            new FormatPatternRule("c", ".", GeneratedRegex.VType.STR),
        };


        GeneratedRegex IRegexGenerator.GenerateRegexFromFormatString(string formatString)
        {
            var ret = new GeneratedRegex();
            ConvertFormatString(formatString, out ret.GeneratedRegexPattern, out ret.FoundArgItems, out ret.LoglineName, out ret.SourceArgName);
            return ret;
        }

        static GeneratedRegex.ArgItem ParsePercentArg(string rhs, out int numCharactersConsumed)
        {
            numCharactersConsumed = 0;
            foreach (RegexGenerator.FormatPatternRule rule in RegexGenerator.formatPatternRules)
            {
                if (rhs.StartsWith(rule.src))
                {
                    string regexPattern = rule.dst;
                    var argItem = new GeneratedRegex.ArgItem(regexPattern, "", rule.vtype);
                    numCharactersConsumed = rule.src.Length;
                    return argItem;
                }
            }
            return null;
        }


        static void AppendEscapedForRegex(char c, StringBuilder buf)
        {
            if (Char.IsLetterOrDigit(c) || c == '_')
            {
                buf.Append(c);
            }
            else
            {
                // Character must be escaped
                buf.Append('\\');
                if ((c == '\n') || (c == '\t'))
                {
                    buf.Append('s');
                }
                else if (Char.IsWhiteSpace(c))
                {
                    buf.Append('s');
                }
                else
                {
                    buf.Append(c);
                }
            }
        }

        private static bool IsNameCharExt(char c)
        {
            return (Char.IsLetterOrDigit(c) || (c == '_') || (c == '-') || (c == '(') || (c == ')'));
        }

        private static string CleanName(string instring)
        {
            StringBuilder sb = new StringBuilder(instring.Length);
            bool sep = false; // mechanism to avoid adding too many underscores when replacing punctuation characters
            foreach (char c in instring)
            {
                if (Char.IsLetterOrDigit(c)) {
                    if (sep)
                    {
                        sb.Append('_');
                        sep = false;
                    }
                    sb.Append(c);
                }
                else
                {
                    sep = true; // Next character will be an underscore
                }
            }
            return sb.ToString();
        }

        // In case instring is " kbps width: " this function will return "width" and set edge=": ".
        static string GetLastTextToken(string instring, out string edge)
        {
            edge = String.Empty; // in case of failure
            if (String.IsNullOrWhiteSpace(instring))
            {
                return String.Empty;
            }
            int iEdge = instring.Length;
            if (instring.EndsWith("0x"))  // hexadecimal prefix shall be ignored
            {
                if (instring.Length <= 2) { return String.Empty; }
                instring = instring.Substring(0, instring.Length - 2);
                iEdge = instring.Length;
            }
            while ((iEdge > 0) && !IsNameCharExt(instring[iEdge - 1]))
            {
                iEdge--;
                if (instring[iEdge] == ',') // Don't use any name beyond of comma
                {
                    return String.Empty;
                }
            }
            if (iEdge == 0)
            {
                return String.Empty; // No word was found
            }
            int iLastComma = instring.LastIndexOf(',');
            int iWord = iEdge;
            if (iLastComma >= 0)
            {
                // Contains comma - Pick all words after.
                // FIXME: This is risky in case there is much text, for example the first argument. How about disabling for first argument?
                iWord = iLastComma + 1;
                while (!IsNameCharExt(instring[iWord])) { iWord++; } // skip until word begins
            }
            else
            {
                // no comma. It is hard to know what belongs to previous argument. Pick last word
                while ((iWord > 0) && IsNameCharExt(instring[iWord - 1]))
                {
                    iWord--;
                }
                // Success
                if (iEdge < instring.Length)
                {
                    edge = instring.Substring(iEdge);
                }
            }
            return instring.Substring(iWord, iEdge - iWord);
        }

        // In case instring is " kbps width: " this function will return "kbps" and set edge=" ".
        static string GetFirstTextToken(string instring, out string edge)
        {
            edge = String.Empty; // in case of failure
            if (String.IsNullOrWhiteSpace(instring))
            {
                return String.Empty;
            }
            int iWord = 0;
            while ((iWord < instring.Length) && !IsNameCharExt(instring[iWord]))
            {
                if (instring[iWord] == ',')
                {
                    return String.Empty; // Don't use anything beyond comma
                }
                iWord++;
            }
            int iEnd = iWord;
            while ((iEnd < instring.Length) && IsNameCharExt(instring[iEnd]))
            {
                iEnd++;
            }
            if (iWord < iEnd)
            {
                // Success
                if (iWord > 0)
                {
                    edge = instring.Substring(0, iWord);
                }
                return instring.Substring(iWord, iEnd - iWord);
            }
            else
            {
                // No word
                return String.Empty;
            }
        }

        static string GuessArgumentName(string left, string right)
        {
            // Extract closest wor and edge(i.e. delimiter or space)
            string edgeL;
            string wordL = GetLastTextToken(left, out edgeL);
            string edgeR;
            string wordR = GetFirstTextToken(right, out edgeR);

            // Is there any word to left or right?
            if (String.IsNullOrWhiteSpace(wordR))
            {
                return wordL; // Have to use the left word. If not exist, return String.Empty
            }
            if (String.IsNullOrWhiteSpace(wordL))
            {
                return wordR; // Have to use the right word. If not exist, return String.Empty
            }

            if (String.IsNullOrWhiteSpace(edgeL) || edgeL.Contains("=") || (edgeL.Contains(":") && !edgeL.EndsWith(" ")))
            {
                // Left word must be used.
                // Examples of format strings leading here: "bitrate%dkbps" "bitrate=%d", "bitrate= %d" "bitrate:%d" or "bitrate:%dkbps"
                return (String.IsNullOrWhiteSpace(edgeR) ?
                    wordL // seems like the format string is something like "bitrate=%d width...."
                    : wordL + '_' + wordR); // Seems like the format string is something like "bitrate=%dkbps"
            }

            if (String.IsNullOrWhiteSpace(edgeR))
            {
                return wordR; // Example format string: " %dkbps"
            }

            if (edgeL.Contains(":"))
            {
                return wordL;
            }

            return wordL + '_' + wordR; // Example format string "bitrate %d width". We don't know which is best, so return "bitrate_width"
        }

        static List<string> GuessArgumentNames(List<string> foundStringsRaw)
        {
            List<string> argumentNames = new List<string>();
            int nArgs = foundStringsRaw.Count - 1;
            for (int i = 0; i < nArgs; i++)
            {
                // Guess name from the strings lefts and right of the value. For example if the format string is "width=%d height..." then then use the pair "width=" and " height..."
                string argName = CleanName(GuessArgumentName(foundStringsRaw[i], foundStringsRaw[i + 1]));
                while (String.IsNullOrWhiteSpace(argName) || !Char.IsLetter(argName[0]) || argumentNames.Contains(argName))
                {
                    argName = "arg" + i.ToString() + '_' + argName;
                }
                argumentNames.Add(argName);
            }
            return argumentNames;
        }


        static String GuessLogLineName(List<string> foundStringsRaw)
        {
            foreach (String raw0 in foundStringsRaw)
            {
                String raw = raw0;
                if (raw.EndsWith("0x"))  // strip hexadecimal prefix
                {
                    raw = raw.Substring(0, raw.Length - 2);
                }
                String clean = CleanName(raw);
                if (!String.IsNullOrWhiteSpace(clean))
                {
                    return clean;
                }
            }
            return "NoName";
        }

        static void ConvertFormatString(string formatString,
            out string generatedRegexPattern,
            out List<GeneratedRegex.ArgItem> foundArgItems,
            out string loglineName,
            out string sourceArgName)
        {
            loglineName = String.Empty; // in case nothing better is found
            sourceArgName = String.Empty;
            //List<RegexGenerator.ArgItem> 
            foundArgItems = new List<GeneratedRegex.ArgItem>();
            List<string> foundStringsEscaped = new List<string>();
            List<string> foundStringsRaw = new List<string>();
            StringBuilder outbufEscaped = new StringBuilder(formatString.Length * 3);
            StringBuilder outbufRaw = new StringBuilder(formatString.Length * 3);
            bool is_escaped = false;
            bool is_percent = false;
            formatString = formatString.Trim();
            for (int i = 0; i < formatString.Length; i++)
            {
                char c = formatString[i];
                if (is_escaped)
                {
                    if (c == 't') // \t
                    {
                        outbufEscaped.Append("\\s+");
                        outbufRaw.Append(' ');
                    }
                    else if (c == 'n') // Ignore \n
                    {
                        outbufRaw.Append(' ');
                    }
                    else
                    {
                        AppendEscapedForRegex(c, outbufEscaped);
                        outbufRaw.Append(c);
                    }
                    is_escaped = false;
                    continue;
                }
                if (is_percent)
                {
                    // Previous character was %. Could it be %d or %s or just %% ?
                    if (c == '%')
                    {
                        AppendEscapedForRegex(c, outbufEscaped); // Double %% ->
                        outbufRaw.Append(c);
                        is_percent = false;
                        continue;
                    }
                    if (!Char.IsLetter(c))
                    {
                        // TODO: Check that c is digit or . or + or space
                        continue; // Skip over
                    }
                    // Yippie! We've found something similar to %d %x
                    // Try all format pattern rules

                    string rhs = formatString.Substring(i);
                    int numCharactersConsumed = 0;
                    var argItem = RegexGenerator.ParsePercentArg(rhs, out numCharactersConsumed);
                    if (argItem == null)
                    {
                        throw new System.NotImplementedException("Invalid format string specifier: %" + rhs);
                    }
                    i += numCharactersConsumed - 1;
                    foundArgItems.Add(argItem);
                    foundStringsEscaped.Add(outbufEscaped.ToString());
                    foundStringsRaw.Add(outbufRaw.ToString());
                    outbufEscaped.Clear();
                    outbufRaw.Clear();
                    is_percent = false;
                    continue;
                }
                if (c == '\\')
                {
                    is_escaped = true;
                    // Don't append to outbuf;
                    continue;
                }
                if (c == '%')
                {
                    is_percent = true;
                    continue;
                }
                // It's a regular message character
                AppendEscapedForRegex(c, outbufEscaped);
                outbufRaw.Append(c);
            }
            foundStringsEscaped.Add(outbufEscaped.ToString());
            foundStringsRaw.Add(outbufRaw.ToString());

            List<string> argumentNames = GuessArgumentNames(foundStringsRaw);

            // Generate full regex string:
            StringBuilder fullRegexPattern = new StringBuilder(formatString.Length * 4);
            fullRegexPattern.Append("^");

            for (int iArg = 0; iArg < foundArgItems.Count; iArg++)
            {
                fullRegexPattern.Append(foundStringsEscaped[iArg]);
                foundArgItems[iArg].name = argumentNames[iArg];
                string dataPattern = "(?<" + argumentNames[iArg] + ">" + foundArgItems[iArg].regexPattern + ")";
                fullRegexPattern.Append(dataPattern);
                if (String.IsNullOrWhiteSpace(sourceArgName) && (foundArgItems[iArg].vtype == GeneratedRegex.VType.STR))
                {
                    sourceArgName = foundArgItems[iArg].name;
                }
            }
            if ((foundStringsRaw.Count > 0) && !String.IsNullOrWhiteSpace(foundStringsRaw[0]))
            {
                loglineName = GuessLogLineName(foundStringsRaw);
            }

            fullRegexPattern.Append(foundStringsEscaped[foundArgItems.Count]);

            fullRegexPattern.Append("$");
            generatedRegexPattern = fullRegexPattern.ToString();
        }
    };
}