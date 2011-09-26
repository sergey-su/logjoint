using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace LogJoint
{
	public class Log4NetImportException : Exception
	{
		public Log4NetImportException(string msg) : base(msg) { }
	};

	public class Log4NetPatternImporter: IDisposable
	{
		StringBuilder headerRe = new StringBuilder();
		StringBuilder bodyRe = new StringBuilder();
		bool inHeader;
		Dictionary<string, int> captureCounters = new Dictionary<string, int>();
		class OutputField
		{
			public StringBuilder Code = new StringBuilder();
			public string CodeType;
		};
		Dictionary<string, OutputField> outputFields = new Dictionary<string, OutputField>();
		CodeDomProvider csharpProvider;

		public static void GenerateRegularGrammarElement(XmlElement root, string pattern)
		{
			using (Log4NetPatternImporter obj = new Log4NetPatternImporter())
				obj.GenerateRegularGrammarElementInt(root, pattern);
		}

		private Log4NetPatternImporter()
		{
			inHeader = true;

			headerRe.AppendLine();

			bodyRe.AppendLine();
			bodyRe.AppendLine("^");
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (csharpProvider != null)
				csharpProvider.Dispose();
		}

		#endregion

		void GenerateRegularGrammarElementInt(XmlElement root, string pattern)
		{
			foreach (PatternToken t in TokenizePattern(pattern))
			{
				if (t.Type == PatternTokenType.Specifier)
					HandleSpecifier(t);
				else
					HandleText(t);
			}
			MakeSureThereIsTimeField();
			FilalizeBodyRe();
			WriteRegularGrammarElement(root);
		}

		enum PatternTokenType
		{
			None, Specifier, Text
		};

		[DebuggerDisplay("{Type} '{Value}'")]
		struct PatternToken
		{ 
			public PatternTokenType Type;
			public string Value;
			public bool RightPaddingFlag;
			public int Padding;
			public int Trim;
			public string Argument;
		};

		// Regexp to match a conversion specifier in a pattern. 
		// Note: all single letter specifiers (like a, c) go after longer specifiers 
		// starting from those letters (appdomain, class).
		// This is importatnt because regexp would match only first letters otherwise.
		static readonly Regex patternParserRe = new Regex(
			@"\%(?:(\%)|(?:(\-?)(\d+))?(?:\.(\d+))?(appdomain|a|class|c|C|date|exception|file|F|identity|location|level|line|logger|l|L|message|mdc|method|m|M|newline|ndc|n|properties|property|p|P|r|timestamp|thread|type|t|username|utcdate|u|w|x|X|d)(?:\{([^\}]+)\})?)",
				RegexOptions.Compiled);

		IEnumerable<PatternToken> TokenizePattern(string pattern)
		{
			int idx = 0; // Current position in the pattern

			for (;;)
			{
				Match m = patternParserRe.Match(pattern, idx);

				if (m.Success)
				{
					// A specifier is found

					if (m.Index > idx) // Yield the text before the specifier found (if any)
					{
						PatternToken txt1 = new PatternToken();
						txt1.Type = PatternTokenType.Text;
						txt1.Value = pattern.Substring(idx, m.Index - idx);
						yield return txt1;
					}

					if (m.Groups[1].Value != "") // If %% was found, yield single '%' 
					{
						PatternToken txt2 = new PatternToken();
						txt2.Type = PatternTokenType.Text;
						txt2.Value = "%";
						yield return txt2;
					}
					else // A 'normal' specifier was found. Fill up the structure and yield it.
					{
						PatternToken spec;
						spec.Type = PatternTokenType.Specifier;
						spec.RightPaddingFlag = m.Groups[2].Value != "";
						spec.Padding = m.Groups[3].Value != "" ? int.Parse(m.Groups[3].Value) : 0;
						spec.Trim = m.Groups[4].Value != "" ? int.Parse(m.Groups[4].Value) : 0;
						spec.Value = m.Groups[5].Value;
						spec.Argument = m.Groups[6].Value;
						yield return spec;
					}

					// Move current position to the end of the specifier found
					idx = m.Index + m.Length;
				}
				else
				{
					if (idx < pattern.Length) // Yield the rest of the pattern if any
					{
						PatternToken txt3 = new PatternToken();
						txt3.Type = PatternTokenType.Text;
						txt3.Value = pattern.Substring(idx);
						yield return txt3;
					}

					// Stop parsing
					break;
				}
			}
		}

		OutputField GetOutputField(string name)
		{
			OutputField ret;
			if (!outputFields.TryGetValue(name, out ret))
				outputFields[name] = ret = new OutputField();
			return ret;
		}

		void ConcatToBody(string code)
		{
			OutputField f = GetOutputField("Body");
			if (f.Code.Length != 0)
				f.Code.Append(" + ");
			f.Code.Append(code);
		}

		CodeDomProvider GetCSharpProvider()
		{
			if (csharpProvider == null)
				csharpProvider = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");
			return csharpProvider;
		}

		string GetCSharpStringLiteral(string value)
		{
			StringWriter sw = new StringWriter();
			GetCSharpProvider().GenerateCodeFromExpression(
				new CodePrimitiveExpression(value), sw, new CodeGeneratorOptions());
			return sw.ToString();
		}

		void HandleText(PatternToken t)
		{
			StringBuilder reToAppend = inHeader ? headerRe : bodyRe;

			reToAppend.AppendFormat("{0} # fixed text '{1}'{2}", Regex.Escape(t.Value), t.Value, Environment.NewLine);

			ConcatToBody(GetCSharpStringLiteral(t.Value));
		}

		string GetDateRe(string format)
		{
			StringBuilder re = new StringBuilder();
			re.AppendLine();
			foreach (string t in TokenizeDatePattern(format))
			{
				switch (t)
				{
					case "d":
						re.AppendLine(@"  \d{1,2} # day of the month");
						break;
					case "dd":
						re.AppendLine(@"  \d{2} # day of the month");
						break;
					case "ddd":
					case "dddd":
						re.AppendLine(@"  \w+ # name of the day");
						break;
					case "f":
					case "ff":
					case "fff":
					case "ffff":
					case "fffff":
					case "ffffff":
					case "fffffff":
						re.AppendFormat(@"  \d{0}{1}{2} # the most significant digits of the seconds fraction{3}", "{", t.Length, "}", Environment.NewLine);
						break;
					case "F":
					case "FF":
					case "FFF":
					case "FFFF":
					case "FFFFF":
					case "FFFFFF":
					case "FFFFFFF":
						re.AppendFormat(@"  (\d{0}{1}{2})? # the most significant digits of the seconds fraction (no trailing zeros){3}", "{", t.Length, "}", Environment.NewLine);
						break; 
					case "g":
					case "gg":
						re.AppendLine(@"  \.+ # the era");
						break;
					case "h":
					case "H":
						re.AppendLine(@"  \d{1,2} # hours");
						break;
					case "hh":
					case "HH":
						re.AppendLine(@"  \d{2} # hours");
						break;
					case "m":
						re.AppendLine(@"  \d{1,2} # minutes");
						break;
					case "mm":
						re.AppendLine(@"  \d{2} # minutes");
						break;
					case "M":
						re.AppendLine(@"  \d{1,2} # month");
						break;
					case "MM":
						re.AppendLine(@"  \d{2} # month");
						break;
					case "MMM":
					case "MMMM":
						re.AppendLine(@"  \w+ # name of month");
						break;
					case "s":
						re.AppendLine(@"  \d{1,2} # seconds");
						break;
					case "ss":
						re.AppendLine(@"  \d{2} # seconds");
						break;
					case "t":
						re.AppendLine(@"  \w # the first character of the A.M./P.M. designator");
						break;
					case "tt":
						re.AppendLine(@"  \w+ # A.M./P.M. designator");
						break;
					case "y":
						re.AppendLine(@"  \d{1,2} # year");
						break;
					case "yy":
						re.AppendLine(@"  \d{2} # year");
						break;
					case "yyyy":
						re.AppendLine(@"  \d{4} # year");
						break;
					case "z":
						re.AppendLine(@"  [\+\-]\d{1,2} # time zone offset");
						break;
					case "zz":
						re.AppendLine(@"  [\+\-]\d{2} # time zone offset");
						break;
					case "zzz":
						re.AppendLine(@"  [\+\-]\d{2}\:\d{2} # time zone offset");
						break;
					default:
						re.AppendFormat("  {0} # fixed string '{1}'{2}", Regex.Escape(t), t, Environment.NewLine);
						break;
				}
			}
			return re.ToString();
		}

		static readonly Regex dateParserRe = new Regex(@"
		(
			(dddd+)|ddd|dd|d| # day of the month
			fffffff|ffffff|fffff|ffff|fff|ff|f| # the N most significant digits of the seconds fraction
			FFFFFFF|FFFFFF|FFFFF|FFFF|FFF|FF|F| # the N most significant digits of the seconds fraction, no trailing zeros
			(gg+)|g| # the era
			(hh+)|h| # 1-12 hour
			(HH+)|h| # 0-24 hour
			(mm+)|m| # minutes
			MMMM|MMM|MM|M| # month
			(ss+)|s| # seconds
			(tt+)|t| # A.M./P.M. designator
			yyyy|yy|y| # year
			(zzz+)|zz|z| # time zone offset 
			\:| # time separator
			\/| # date separator
			\\(\.) # escape character
		)
		", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		IEnumerable<string> TokenizeDatePattern(string pattern)
		{
			int idx = 0;
			for (; ; )
			{
				Match m = dateParserRe.Match(pattern, idx);
				if (m.Success)
				{
					if (m.Index > idx) // Yield the text before the specifier found (if any)
					{
						yield return pattern.Substring(idx, m.Index - idx);
					}

					if (m.Groups[2].Value != "")
						yield return "dddd";
					else if (m.Groups[3].Value != "")
						yield return "gg";
					else if (m.Groups[4].Value != "")
						yield return "hh";
					else if (m.Groups[5].Value != "")
						yield return "HH";
					else if (m.Groups[6].Value != "")
						yield return "mm";
					else if (m.Groups[7].Value != "")
						yield return "ss";
					else if (m.Groups[8].Value != "")
						yield return "tt";
					else if (m.Groups[9].Value != "")
						yield return "zzz";
					else if (m.Groups[10].Value != "")
						yield return m.Groups[10].Value;
					else
						yield return m.Groups[1].Value;

					idx = m.Index + m.Length;
				}
				else
				{
					if (idx < pattern.Length) // Yield the rest of the pattern if any
					{
						yield return pattern.Substring(idx);
					}

					// Stop parsing
					break;
				}
			}
		}

		void HandleSpecifier(PatternToken t)
		{
			string captureName;
			string re;
			string outputFieldName = null;
			string outputFieldCode = null;
			string outputFieldCodeType = null;

			switch (t.Value)
			{
				case "a":
				case "appdomain":
					captureName = "AppDomain";
					re = @".*";
					break;
				case "c":
				case "logger":
					captureName = "Logger";
					re = @"[\w\.]+";
					break;
				case "C":
				case "class":
				case "type":
					captureName = "Class";
					re = @".*"; // * because caller location might not be accessible
					break;
				case "d":
				case "date":
				case "utcdate":
					captureName = "Time";
					string fmt;
					switch (t.Argument)
					{
						case "ABSOLUTE":
							fmt = "HH:mm:ss,fff";
							break;
						case "DATE":
							fmt = "dd MMM yyyy HH:mm:ss,fff";
							break;
						case "ISO8601":
						case "":
							fmt = "yyyy-MM-dd HH:mm:ss,fff";
							break;
						default:
							fmt = t.Argument;
							break;
					}
					re = GetDateRe(fmt);
					outputFieldName = "Time";
					outputFieldCode = string.Format("TO_DATETIME(Time, {0})", GetCSharpStringLiteral(fmt));
					break;
				case "newline":
				case "n":
					captureName = "NL";
					re = @"\n|\r\n";
					break;
				case "exception":
					captureName = "Exception";
					re = @".*";
					inHeader = false;
					break;
				case "file":
				case "F":
					captureName = "File";
					// insert " with Format() because otherwise I would have to remove @ and escape the re heavily
					re = string.Format(@"[^\*\?\{0}\<\>\|]*", '"'); // [...]* because caller location might not be accessible
					break;
				case "identity":
				case "u":
					captureName = "Identity";
					re = @"[\w\\\.]*";
					break;
				case "location":
				case "l":
					captureName = "Location";
					re = @".*"; // * because caller location might not be accessible
					break;
				case "line":
				case "L":
					captureName = "Line";
					re = @"\d*"; // * because caller location might not be accessible
					break;
				case "p":
				case "level":
					captureName = "Level";
					re = @"DEBUG|INFO|WARN|ERROR|FATAL";
					outputFieldName = "Severity";
					outputFieldCode = @"
switch (Level)
{
case ""WARN"":
	return Severity.Warning;
case ""ERROR"":
case ""FATAL"":
	return Severity.Error;
default:
	return Severity.Info;
}";
					outputFieldCodeType = "function";
					break;
				case "message":
				case "m":
					captureName = "Message";
					inHeader = false;
					re = ".*";
					break;
				case "method":
				case "M":
					captureName = "Method";
					re = ".*";
					break;
				case "mdc":
				case "property":
				case "P":
				case "X":
				case "properties":
					captureName = "Prop";
					re = ".*";
					break;
				case "r":
				case "timestamp":
					captureName = "Timestamp";
					re = @"\d+";
					break;
				case "thread":
				case "t":
					captureName = "Thread";
					re = ".+";
					outputFieldName = "Thread";
					outputFieldCode = "Thread";
					break;
				case "username":
				case "w":
					captureName = "User";
					re = @"[\w\\\.]+";
					break;
				case "x":
				case "ndc":
					captureName = "NDC";
					re = @".+";
					break;
				default:
					return;
			}

			captureName = RegisterCaptureName(captureName);

			StringBuilder reToAppend = inHeader ? headerRe : bodyRe;

			string paddingString = null;
			if (t.Padding != 0)
				paddingString = string.Format(@"\ {{0,{0}}}", t.Padding);

			if (paddingString != null && !t.RightPaddingFlag)
				reToAppend.Append(paddingString);

			reToAppend.AppendFormat("(?<{0}>{1})", captureName, re);

			if (paddingString != null && t.RightPaddingFlag)
				reToAppend.Append(paddingString);

			reToAppend.AppendFormat(" # field '{0}'{1}", t.Value, Environment.NewLine);

			if (outputFieldName != null)
			{
				OutputField f = GetOutputField(outputFieldName);

				// First, check is the code is empty yet.
				// The code may have already been initialized. That may happen if
				// there are several specifiers with the same name and that produce
				// an output field. We want to take in use only the first such specifier. 
				if (f.Code.Length == 0) 
				{
					f.Code.Append(outputFieldCode);
					f.CodeType = outputFieldCodeType;

					// I think time field souldn't go to the body because
					// it can be displayed later in the log view (Show Time... in content menu)
					if (outputFieldName == "Time")
						return;
				}
			}
			ConcatToBody(captureName);
		}

		string RegisterCaptureName(string captureName)
		{
			int captureCounter;
			if (!captureCounters.TryGetValue(captureName, out captureCounter))
				captureCounter = 0;
			captureCounter++;
			captureCounters[captureName] = captureCounter;

			if (captureCounter > 1)
				captureName += captureCounter;

			return captureName;
		}

		void WriteRegularGrammarElement(XmlElement root)
		{
			XmlNode regGram = root.SelectSingleNode("regular-grammar");

			XmlNode headerReNode = regGram.SelectSingleNode("head-re");
			headerReNode.RemoveAll();
			headerReNode.AppendChild(root.OwnerDocument.CreateCDataSection(headerRe.ToString()));

			XmlNode bodyReNode = regGram.SelectSingleNode("body-re");
			bodyReNode.RemoveAll();
			bodyReNode.AppendChild(root.OwnerDocument.CreateCDataSection(bodyRe.ToString()));

			XmlNode fieldsConfig = regGram.SelectSingleNode("fields-config");
			fieldsConfig.RemoveAll();

			WriteFields(fieldsConfig);
		}

		void MakeSureThereIsTimeField()
		{
			if (outputFields.ContainsKey("Time"))
				return;
			OutputField f = new OutputField();
			f.Code.Append("DateTime.Now");
			outputFields["Time"] = f;
		}

		void FilalizeBodyRe()
		{
			bodyRe.AppendLine(@"(?<Extra>.*)");
			bodyRe.AppendLine(@"$");
			ConcatToBody("(Extra.Length != 0 ? Environment.NewLine + Extra : \"\")");
		}

		void WriteFields(XmlNode fieldsConfig)
		{
			foreach (KeyValuePair<string, OutputField> f in outputFields)
			{
				XmlElement fldElem = fieldsConfig.OwnerDocument.CreateElement("field");
				fieldsConfig.AppendChild(fldElem);
				fldElem.SetAttribute("name", f.Key);
				if (f.Value.CodeType != null)
					fldElem.SetAttribute("code-type", f.Value.CodeType);
				fldElem.InnerText = f.Value.Code.ToString();
			}
		}
	}
}
