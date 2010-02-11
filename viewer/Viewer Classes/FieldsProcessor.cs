using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public interface IMessagesBuilderCallback
	{
		long CurrentPosition { get; }
		IThread GetThread(string id);
	};

	public class FieldsProcessorException : Exception
	{
		public FieldsProcessorException(string message, CompilerErrorCollection errors, string code):
			base(message)
		{
			Errors = errors;
			Code = code;
		}

		public readonly CompilerErrorCollection Errors;
		public readonly string Code;

	};

	public abstract class MessageBuilderFunctions
	{
		public string TRIM(string str)
		{
			return FieldsProcessor.TrimInsignificantSpace(str);
		}

		public int HEX_TO_INT(string str)
		{
			return int.Parse(str, NumberStyles.HexNumber);
		}

		public DateTime TO_DATETIME(string value, string format)
		{
			try
			{
				return DateTime.ParseExact(value, format,
					CultureInfo.InvariantCulture.DateTimeFormat);
			}
			catch (FormatException e)
			{
				throw new FormatException(string.Format("{0}. Format={1}, Value={2}", e.Message,
					format, value));
			}
		}

		public int PARSE_YEAR(string year)
		{
			int y = Int32.Parse(year);
			if (y < 100)
			{
				if (y < 60)
					return 2000 + y;
				return 1900 + y;
			}
			return y;
		}

		public string DEFAULT_DATETIME_FORMAT()
		{
			//return "yyyy-MM-ddTHH:mm:ss.fff";
			//2009-08-07 13:17:55
			return "yyyy-MM-dd HH:mm:ss";
		}

		public DateTime EPOCH_TIME(long epochTime)
		{
			DateTime ret = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return ret.ToLocalTime().AddMilliseconds(epochTime);
		}

		public string NEW_LINE()
		{
			return Environment.NewLine;
		}

		public DateTime DATETIME_FROM_TIMEOFDAY(DateTime timeOfDay)
		{
			DateTime tmp = SOURCE_TIME();
			return new DateTime(tmp.Year, tmp.Month, tmp.Day, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
		}

		protected abstract DateTime SOURCE_TIME();
	};

	public class FieldsProcessor
	{
		public abstract class MessageBuilder : MessageBuilderFunctions
		{
			internal List<string> __fields = new List<string>();
			internal List<string> __names = new List<string>();
			internal DateTime __sourceTime;

			protected int INPUT_FIELDS_COUNT()
			{
				return __fields.Count;
			}

			protected string INPUT_FIELD_VALUE(int idx)
			{
				return __fields[idx];
			}

			protected string INPUT_FIELD_NAME(int idx)
			{
				return __names[idx];
			}

			protected override DateTime SOURCE_TIME()
			{
				return __sourceTime;
			}

			public enum Severity
			{
				Info = MessageBase.MessageFlag.Info,
				Warning = MessageBase.MessageFlag.Warning,
				Error = MessageBase.MessageFlag.Error,
			};

			public enum EntryType
			{
				Content,
				FrameBegin,
				FrameEnd,
			};

			public abstract MessageBase MakeMessage(IMessagesBuilderCallback callback);
		};

		public static string TrimInsignificantSpace(string str)
		{
			return str.Trim(InsignificantSpaces);
		}

		static readonly char[] InsignificantSpaces = new char[] { '\t', '\n', '\r', ' ' };

		public FieldsProcessor(XmlNode e, bool performChecks)
		{
			foreach (XmlElement f in e.SelectNodes("field"))
			{
				OutputFieldStruct s;
				s.Name = string.Intern(f.GetAttribute("name"));
				switch (f.GetAttribute("code-type"))
				{
					case "function":
						s.Type = OutputFieldStruct.CodeType.Function;
						break;
					default:
						s.Type = OutputFieldStruct.CodeType.Expression;
						break;
				}
				s.Code = f.InnerText;
				outputFields.Add(s);
				if (s.Name == "Time")
					timeField = s;
			}
			if (performChecks)
			{
				if (timeField.Name == null)
					throw new Exception("'Time' field is not defined");
			}
		}

		public FieldsProcessor(FieldsProcessor other)
		{
			outputFields.AddRange(other.outputFields);
			extensions.AddRange(other.extensions);
			timeField = other.timeField;
		}

		public string GetTimeFieldCode()
		{
			return timeField.Code;
		}

		public void Reset()
		{
			if (builder != null)
			{
				for (int i = 0; i < builder.__fields.Count; ++i)
					builder.__fields[i] = null;
				builder.__sourceTime = new DateTime();
			}
			else
			{
				this.inputFields.Clear();
				this.sourceTime = new DateTime();
			}
		}

		public void SetSourceTime(DateTime sourceTime)
		{
			if (builder != null)
				builder.__sourceTime = sourceTime;
			else
				this.sourceTime = sourceTime;
		}

		public void SetInputField(int idx, string name, string value)
		{
			if (builder != null)
			{
				builder.__fields[idx] = value;
			}
			else
			{
				InputFieldStruct s;
				s.Name = name;
				s.Value = value;
				s.Index = idx;
				while (idx >= inputFields.Count)
					inputFields.Add(new InputFieldStruct());
				inputFields[idx] = s;
			}
		}

		public struct ProcessorExtention
		{
			public string FieldName;
			public string ClassName;
			public ProcessorExtention(string fieldName, string className)
			{
				FieldName = fieldName;
				ClassName = className;
			}
		};

		public void AddExtension(ProcessorExtention ext)
		{
			extensions.Add(ext);
		}

		public MessageBase MakeMessage(IMessagesBuilderCallback callback)
		{
			Compile();
			return builder.MakeMessage(callback);
		}

		public void Compile()
		{
			if (builder != null)
			{
				return;
			}
			builder = CreateBuilder();
			foreach (InputFieldStruct s in inputFields)
			{
				builder.__fields.Add(s.Value);
				builder.__names.Add(s.Name);
			}
			builder.__sourceTime = sourceTime;
		}

		static readonly Regex escapeFieldNameRe = new Regex(@"[^\w]", RegexOptions.Compiled);

		static string EscapeFieldName(string name)
		{
			return escapeFieldNameRe.Replace(name, "_");
		}

		static string GetOutputFieldExpression(OutputFieldStruct s, string type, StringBuilder helperFunctions)
		{
			switch (s.Type)
			{
				case OutputFieldStruct.CodeType.Expression:
					return string.Format("{0}{2}{1}", UserCode.GetProlog(s.Name), UserCode.GetEpilog(s.Name), s.Code);
				case OutputFieldStruct.CodeType.Function:
					string helperFuncName = "__Get_" + EscapeFieldName(s.Name);
					helperFunctions.AppendFormat(@"
	{0} {1}()
	{{
{4}{2}{5}
	}}{3}", 
					type, helperFuncName, s.Code, Environment.NewLine,
					UserCode.GetProlog(s.Name), UserCode.GetEpilog(s.Name));
					return helperFuncName + "()";
				default:
					Debug.Assert(false);
					return "";
			}
		}

		MessageBuilder CreateBuilder()
		{
			StringBuilder helperFunctions = new StringBuilder();

			StringBuilder code = new StringBuilder();
			code.AppendLine(@"
using System;
using System.Text;

public class MessageBuilder: LogJoint.FieldsProcessor.MessageBuilder
{");

			List<Assembly> refs = new List<Assembly>();

			foreach (InputFieldStruct s in inputFields)
			{
				code.AppendFormat(@"
	string {0} {{ get {{ return INPUT_FIELD_VALUE({1}); }} }}{2}", 
				 string.IsNullOrEmpty(s.Name) ? "Field" + s.Index.ToString() : s.Name, 
				 s.Index,
				 Environment.NewLine);
			}

			foreach (ProcessorExtention ext in extensions)
			{
				Type extType = Type.GetType(ext.ClassName);
				if (extType == null)
					throw new Exception("Type of extension not found: " + ext.ClassName);
				code.AppendFormat(@"
	{0} {1} = new {0}();{2}",
				 extType.FullName, ext.FieldName, Environment.NewLine);
				refs.Add(extType.Assembly);
			}

			code.AppendLine(@"
	public override LogJoint.MessageBase MakeMessage(LogJoint.IMessagesBuilderCallback __callback)
	{
			");

			bool timeAdded = false;
			bool bodyAdded = false;
			bool threadAdded = false;
			bool fieldsAdded = false;
			bool severityAdded = false;
			bool typeAdded = false;

			foreach (OutputFieldStruct s in outputFields)
			{
				string predefinedFieldVar = null;
				string predefinedFieldType = null;
				switch (s.Name)
				{
					case "Time":
						predefinedFieldVar = "__time";
						predefinedFieldType = "DateTime";
						timeAdded = true;
						break;
					case "Body":
						predefinedFieldVar = "__body";
						predefinedFieldType = "string";
						bodyAdded = true;
						break;
					case "Thread":
						predefinedFieldVar = "__thread";
						predefinedFieldType = "string";
						threadAdded = true;
						break;
					case "Severity":
						predefinedFieldVar = "__severity";
						predefinedFieldType = "Severity";
						severityAdded = true;
						break;
					case "EntryType":
						predefinedFieldVar = "__entryType";
						predefinedFieldType = "EntryType";
						typeAdded = true;
						break;
				}
				if (predefinedFieldVar != null)
				{
					code.AppendFormat(@"
		{0} {1} = {2};", predefinedFieldType, predefinedFieldVar, 
							GetOutputFieldExpression(s, predefinedFieldType, helperFunctions));
				}
				else
				{
					if (!fieldsAdded)
					{
						code.AppendLine(@"
		StringBuilder __fields = new StringBuilder();
		");
						fieldsAdded = true;
					}
					code.AppendFormat(@"
		__fields.AppendFormat(""{{0}}{{1}}={{2}}"", Environment.NewLine, ""{0}"", {1});",
						s.Name, GetOutputFieldExpression(s, "string", helperFunctions));
				}
			}

			if (!timeAdded)
			{
				code.AppendLine(@"
		DateTime __time = DateTime.MinValue;");
			}

			if (!bodyAdded)
			{
				code.AppendLine(@"
		string __body = """";");
			}

			if (!threadAdded)
			{
				code.AppendLine(@"
		string __thread = """";");
			}

			if (!severityAdded)
			{
				code.AppendLine(@"
		Severity __severity = Severity.Info;");
			}

			if (!typeAdded)
			{
				code.AppendLine(@"
		EntryType __entryType = EntryType.Content;");
			}

			code.AppendLine(@"
		__body = TRIM(__body);");


			if (fieldsAdded)
			{
				code.AppendLine(@"
		__body += __fields.ToString();");
			}

			code.AppendLine(@"
		LogJoint.IThread mtd = __callback.GetThread(__thread);

		switch (__entryType)
		{
		case EntryType.FrameBegin:
			return new LogJoint.FrameBegin(
				__callback.CurrentPosition,
				mtd, 
				__time, 
				__body);
		case EntryType.FrameEnd:
			return new LogJoint.FrameEnd(
				__callback.CurrentPosition,
				mtd, 
				__time);
		default:
			return new LogJoint.Content(
				__callback.CurrentPosition,
				mtd,
				__time,
				__body,
				(LogJoint.Content.SeverityFlag)__severity
			);
		}");

			code.AppendLine(@"
	}");

			code.Append(helperFunctions.ToString());

			code.AppendLine(@"
}");

			using (CSharpCodeProvider prov = new CSharpCodeProvider())
			{
				CompilerParameters cp = new CompilerParameters();
				cp.GenerateInMemory = true;
				cp.ReferencedAssemblies.Add("System.dll");
				cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
				foreach (Assembly refAsm in refs)
					cp.ReferencedAssemblies.Add(refAsm.Location);
				cp.CompilerOptions = "/optimize";
				string tempDir = TempFilesManager.GetInstance(Source.EmptyTracer).GenerateNewName();
				Directory.CreateDirectory(tempDir);
				try
				{
					cp.TempFiles = new TempFileCollection(tempDir);
					CompilerResults cr = prov.CompileAssemblyFromSource(cp, code.ToString());
					if (cr.Errors.HasErrors)
					{
						StringBuilder sb = new StringBuilder();
						sb.Append("Failed to process log fields. There must be an error in format's configuration. ");
						foreach (CompilerError err in cr.Errors)
						{
							if (!err.IsWarning)
								sb.AppendLine(err.ErrorText);
						}
						throw new FieldsProcessorException(sb.ToString(), cr.Errors, code.ToString());
					}
					Type fieldsType = cr.CompiledAssembly.GetType("MessageBuilder");
					return (MessageBuilder)Activator.CreateInstance(fieldsType);
				}
				finally
				{
					if (Directory.Exists(tempDir))
						Directory.Delete(tempDir, true);
				}
			}
		}

		MessageBuilder builder;

		[DebuggerDisplay("{Name}={Value}")]
		struct InputFieldStruct
		{
			public int Index;
			public string Name;
			public string Value;
		};
		DateTime sourceTime;
		List<InputFieldStruct> inputFields = new List<InputFieldStruct>();

		struct OutputFieldStruct
		{
			public enum CodeType
			{
				Expression,
				Function
			};
			public string Name;
			public CodeType Type;
			public string Code;
		};
		List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
		OutputFieldStruct timeField;
		List<ProcessorExtention> extensions = new List<ProcessorExtention>();
	};

	static class UserCode
	{
		public static string GetProlog(string fieldName)
		{
			return string.Format("/* User code begin. Field: {0} */ ", fieldName);
		}

		public static string GetEpilog(string fieldName)
		{
			return " /* User code end */";
		}

		public struct Entry
		{
			public string UserCode;
			public int Index, Length;
			public string FieldName;
		};

		static readonly Regex userCodeRe = new Regex(@"\/\* User code begin\. Field: (.+?) \*\/ (.+?) \/\* User code end \*\/", 
			RegexOptions.Compiled | RegexOptions.Singleline);

		static public IEnumerable<Entry> GetEntries(string code)
		{
			int pos = 0;
			for (; ; )
			{
				Match m = userCodeRe.Match(code, pos);
				if (!m.Success)
					break;
				pos = m.Index + m.Length;

				Entry ret;
				ret.Index = m.Groups[2].Index;
				ret.Length = m.Groups[2].Length;
				ret.FieldName = m.Groups[1].Value;
				ret.UserCode = m.Groups[2].Value;
				yield return ret;

			}
		}

		static readonly Regex newLineRe = new Regex(@"^.*$", RegexOptions.Compiled | RegexOptions.Multiline);

		public struct LineInfo
		{
			public int LineNumber;
			public int Position, Length;
		};

		static public IEnumerable<LineInfo> GetLines(string code)
		{
			int pos = 0;
			for (int lineNum = 1; ; ++lineNum)
			{
				Match m = newLineRe.Match(code, pos);
				if (!m.Success)
					break;

				pos = m.Index + m.Length;

				LineInfo ret;
				ret.Position = m.Index;
				ret.Length = m.Length;
				ret.LineNumber = lineNum;
				yield return ret;

				if (m.Length == 0)
					break;
			}
		}
	}
}
