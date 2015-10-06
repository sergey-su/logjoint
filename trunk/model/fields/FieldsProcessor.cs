using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
#if !SILVERLIGHT
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public interface IMessagesBuilderCallback
	{
		long CurrentPosition { get; }
		IThread GetThread(StringSlice id);
	};

	[Flags]
	public enum MakeMessageFlags
	{
		Default = 0,
		HintIgnoreTime = 1,
		HintIgnoreBody = 2,
		HintIgnoreSeverity = 4,
		HintIgnoreThread = 8,
		HintIgnoreEntryType = 16,
	};

	public partial class FieldsProcessor
	{
		public class InitializationParams
		{
			public InitializationParams(XElement fieldsNode, bool performChecks, Type precompiledUserCode)
			{
				if (fieldsNode == null)
					throw new ArgumentNullException("fieldsNode");
				foreach (XElement f in fieldsNode.Elements("field"))
				{
					OutputFieldStruct s;
					s.Name = string.Intern(f.Attribute("name").Value);
					var codeTypeAttr = f.Attribute("code-type");
					switch (codeTypeAttr != null ? codeTypeAttr.Value : "")
					{
						case "function":
							s.Type = OutputFieldStruct.CodeType.Function;
							break;
						default:
							s.Type = OutputFieldStruct.CodeType.Expression;
							break;
					}
					s.Code = f.Value;
					this.outputFields.Add(s);
					if (s.Name == "Time")
						this.timeField = s;
				}
				if (performChecks)
				{
					if (this.timeField.Name == null)
						throw new Exception("'Time' field is not defined");
				}
				this.precompiledUserCode = precompiledUserCode;
			}

			internal void InitializeInstance(FieldsProcessor proc)
			{
				proc.outputFields.AddRange(outputFields);
				proc.timeField = timeField;
				proc.precompiledBuilderType = precompiledUserCode;
			}

			readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
			readonly OutputFieldStruct timeField;
			readonly Type precompiledUserCode;
		};

		public struct ExtensionInfo
		{
			public readonly string ExtensionName;
			public readonly string ExtensionAssemblyName;
			public readonly string ExtensionClassName;
			public readonly Func<object> InstanceGetter;
			public ExtensionInfo(string extensionName, string extensionAssemblyName, string extensionClassName, 
				Func<object> instanceGetter)
			{
				if (string.IsNullOrEmpty(extensionName))
					throw new ArgumentException("extensionName");
				if (string.IsNullOrEmpty(extensionAssemblyName))
					throw new ArgumentException("extensionAssemblyName");
				if (string.IsNullOrEmpty(extensionClassName))
					throw new ArgumentException("extensionClassName");
				if (instanceGetter == null)
					throw new ArgumentNullException("instanceGetter");
				if (!StringUtils.IsValidCSharpIdentifier(extensionName))
					throw new ArgumentException("extensionName must be a valid C# identifier", "extensionName");

				this.ExtensionName = extensionName;
				this.ExtensionAssemblyName = extensionAssemblyName;
				this.ExtensionClassName = extensionClassName;
				this.InstanceGetter = instanceGetter;
			}
		};

		public FieldsProcessor(
			InitializationParams initializationParams, 
			IEnumerable<string> inputFieldNames, 
			IEnumerable<ExtensionInfo> extensions)
		{
			if (inputFieldNames == null)
				throw new ArgumentNullException("inputFieldNames");
			initializationParams.InitializeInstance(this);
			if (extensions != null)
				this.extensions.AddRange(extensions);
			this.inputFieldNames = inputFieldNames.Select((name, idx) => name ?? string.Format("Field{0}", idx)).ToList();
		}

		public void Reset()
		{
			if (builder == null)
				builder = CreateBuilderInstance();

			builder.ResetFieldValues();
			builder.__sourceTime = new DateTime();
			builder.__position = 0;
			builder.__timeOffset = new TimeSpan();
		}

		public void SetSourceTime(DateTime sourceTime)
		{
			builder.__sourceTime = sourceTime;
		}

		public void SetPosition(long value)
		{
			builder.__position = value;
		}

		public void SetTimeOffset(TimeSpan value)
		{
			builder.__timeOffset = value;
		}

		public void SetInputField(int idx, StringSlice value)
		{
			builder.SetInputFieldByIndex(idx, value);
		}

		public IMessage MakeMessage(IMessagesBuilderCallback callback, MakeMessageFlags flags)
		{
			return builder.MakeMessage(callback, flags);
		}

		public Type CompileUserCodeToType(CompilationTargetFx targetFx, Func<string, string> assemblyLocationResolver)
		{
			return CompileUserCodeToTypeInternal(targetFx, assemblyLocationResolver);
		}

		#region Implementation

		static readonly Regex escapeFieldNameRe = new Regex(@"[^\w]");

		static string EscapeFieldName(string name)
		{
			return escapeFieldNameRe.Replace(name, "_");
		}

		/// <summary>
		/// Calculates an integer hash out of all fields that the message builder type depends on
		/// </summary>
		int GetMessageBuilderTypeHash(List<string> inputFieldNames)
		{
			int typeHash = 0;
			foreach (string i in inputFieldNames)
			{
				typeHash ^= i.GetHashCode();
			}
			foreach (OutputFieldStruct i in outputFields)
			{
				typeHash ^= (int)i.Type ^ i.Name.GetHashCode() ^ i.Code.GetHashCode();
			}
			foreach (ExtensionInfo i in extensions)
			{
				typeHash ^= i.ExtensionAssemblyName.GetType().GetHashCode() ^ i.ExtensionClassName.GetType().GetHashCode() ^  i.ExtensionName.GetHashCode();
			}
			return typeHash;
		}

		Internal.__MessageBuilder CreateBuilderInstance()
		{
			Type builderType = precompiledBuilderType;

			if (builderType == null)
			{
				int builderTypeHash = GetMessageBuilderTypeHash(inputFieldNames);

				lock (builderTypesCache)
				{
					if (!builderTypesCache.TryGetValue(builderTypeHash, out builderType))
					{
						builderType = CompileUserCodeToTypeInternal(CompilationTargetFx.RunningFx,
							asmName => Assembly.Load(asmName).Location);
						builderTypesCache.Add(builderTypeHash, builderType);
					}
				}
			}

			Internal.__MessageBuilder ret = (Internal.__MessageBuilder)Activator.CreateInstance(builderType);

			foreach (ExtensionInfo ext in extensions)
				ret.SetExtensionByName(ext.ExtensionName, ext.InstanceGetter());

			return ret;
		}

		static Dictionary<int, Type> builderTypesCache = new Dictionary<int, Type>();

#if SILVERLIGHT
		Type CompileUserCodeToTypeInternal(CompilationTargetFx targetFx, Func<string, string> assemblyLocationResolver)
		{
			throw new NotImplementedException("Code compilaction not supported in silverlight");
		}
#else
		static string GetSilverlightDir()
		{
			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Silverlight", false))
			{
				if (key != null)
				{
					var ver = key.GetValue("Version") as string;
					if (!string.IsNullOrEmpty(ver))
					{
						string dir = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft Silverlight\") + ver + @"\";
						if (System.IO.Directory.Exists(dir))
							return dir;
					}
				}
			}
			throw new Exception("Silverlight not found on this machine");
		}

		Type CompileUserCodeToTypeInternal(CompilationTargetFx targetFx, Func<string, string> assemblyLocationResolver)
		{
			List<string> refs = new List<string>();
			string fullCode = MakeMessagesBuilderCode(inputFieldNames, extensions, outputFields, refs);
		
			using (CSharpCodeProvider prov = new CSharpCodeProvider())
			{
				CompilerParameters cp = new CompilerParameters();
				if (targetFx == CompilationTargetFx.RunningFx)
				{
					cp.ReferencedAssemblies.Add("System.dll");
					cp.CompilerOptions = "/optimize";
				}
				else if (targetFx == CompilationTargetFx.Silverlight)
				{
					var silverLightDir = GetSilverlightDir();
					foreach (var silverlightAsm in new string[] {"mscorlib.dll", "System.Core.dll", "System.dll"})
						cp.ReferencedAssemblies.Add(silverLightDir + silverlightAsm);
					cp.CompilerOptions = "/optimize /nostdlib+";
				}
				List<string> resolvedRefs = new List<string>();
				resolvedRefs.Add(assemblyLocationResolver(Assembly.GetExecutingAssembly().FullName));
				foreach (string refAsm in refs)
					resolvedRefs.Add(assemblyLocationResolver(refAsm));

				foreach (string resoledAsm in resolvedRefs)
					cp.ReferencedAssemblies.Add(resoledAsm);

				string tempDir = TempFilesManager.GetInstance().GenerateNewName();
				Directory.CreateDirectory(tempDir);
				cp.OutputAssembly = string.Format("{0}{2}UserCode{1}.dll", tempDir, Guid.NewGuid().ToString("N"), Path.DirectorySeparatorChar);
				// Temp folder will be cleaned when LogJoint starts next time.
				cp.TempFiles = new TempFileCollection(tempDir, false);
				cp.TreatWarningsAsErrors = false;
				CompilerResults cr;
				using (new CodedomEnvironmentConfigurator())
				{
					cr = prov.CompileAssemblyFromSource(cp, fullCode);
				}
				if (cr.Errors.HasErrors)
					ThrowBadUserCodeException(fullCode, cr);
				return cr.CompiledAssembly.GetType("GeneratedMessageBuilder");
			}	
		}

		static string GetOutputFieldExpression(OutputFieldStruct s, string type, StringBuilder helperFunctions)
		{
			bool retTypeIsStringSlice = type == "StringSlice";
			switch (s.Type)
			{
				case OutputFieldStruct.CodeType.Expression:
					var fmt = retTypeIsStringSlice ? "new StringSlice({0}{2}{1})" : "{0}{2}{1}";
					return string.Format(fmt, UserCode.GetProlog(s.Name), UserCode.GetEpilog(s.Name), s.Code);
				case OutputFieldStruct.CodeType.Function:
					var helperCallFmt = retTypeIsStringSlice ? "new StringSlice({0}())" : "{0}()";
					var helperRetType = retTypeIsStringSlice ? "string" : type;
					string helperFuncName = "__Get_" + EscapeFieldName(s.Name);
					helperFunctions.AppendFormat(@"
	{0} {1}()
	{{
{4}{2}{5}
	}}{3}",
					helperRetType, helperFuncName, s.Code, Environment.NewLine,
					UserCode.GetProlog(s.Name), UserCode.GetEpilog(s.Name));
					return string.Format(helperCallFmt, helperFuncName);
				default:
					Debug.Assert(false);
					return "";
			}
		}

		static string MakeMessagesBuilderCode(List<string> inputFieldNames,
			List<ExtensionInfo> extensions, List<OutputFieldStruct> outputFields, List<string> refs)
		{
			StringBuilder helperFunctions = new StringBuilder();

			StringBuilder code = new StringBuilder();
			code.AppendLine(@"
using System;
using System.Text;
using LogJoint;

public class GeneratedMessageBuilder: LogJoint.Internal.__MessageBuilder
{");

			for (int i = 0; i < inputFieldNames.Count; ++i)
			{
				code.AppendFormat(@"
	StringSlice {0};{1}", inputFieldNames[i], Environment.NewLine);
				code.AppendFormat(@"
	string {0}String {{ get {{ return {0}.Value; }} }}{1}", inputFieldNames[i], Environment.NewLine);
			}

			code.AppendFormat(@"
	public override void SetInputFieldByIndex(int __index, StringSlice __value)
	{{
		switch (__index)
		{{");
			for (int i = 0; i < inputFieldNames.Count; ++i)
			{
				code.AppendFormat(@"
			case {0}: {1} = __value; break;{2}", i, inputFieldNames[i], Environment.NewLine);
			}
			code.AppendFormat(@"
		}}
	}}{0}", Environment.NewLine);

			code.AppendFormat(@"
	public override void ResetFieldValues()
	{{");
			for (int i = 0; i < inputFieldNames.Count; ++i)
			{
				code.AppendFormat(@"
		{0} = StringSlice.Empty;{1}", inputFieldNames[i], Environment.NewLine);
			}
			code.AppendFormat(@"
	}}{0}", Environment.NewLine);

			code.AppendFormat(@"
	protected override int INPUT_FIELDS_COUNT()
	{{");
			code.AppendFormat(@"
		return {0}{1};", inputFieldNames.Count, Environment.NewLine);
			code.AppendFormat(@"
	}}{0}", Environment.NewLine);

			code.AppendFormat(@"
	protected override StringSlice INPUT_FIELD_VALUE(int __index)
	{{
		switch (__index)
		{{");
			for (int i = 0; i < inputFieldNames.Count; ++i)
			{
				code.AppendFormat(@"
			case {0}: return {1};{2}", i, inputFieldNames[i], Environment.NewLine);
			}
			code.AppendFormat(@"
			default: return new StringSlice();
		}}
	}}{0}", Environment.NewLine);

			code.AppendFormat(@"
	protected override string INPUT_FIELD_NAME(int __index)
	{{
		switch (__index)
		{{");
			for (int i = 0; i < inputFieldNames.Count; ++i)
			{
				code.AppendFormat(@"
			case {0}: return {3}{1}{3};{2}", i, inputFieldNames[i], Environment.NewLine, "\"");
			}
			code.AppendFormat(@"
			default: return string.Empty;
		}}
	}}{0}", Environment.NewLine);

			foreach (ExtensionInfo ext in extensions)
			{
				code.AppendFormat(@"
	{0} {1};{2}",
				 ext.ExtensionClassName, ext.ExtensionName, Environment.NewLine);
				refs.Add(ext.ExtensionAssemblyName);
			}

			code.AppendLine(@"
	public override void SetExtensionByName(string __name, object __ext)
	{
		switch (__name)
		{");
			foreach (ExtensionInfo ext in extensions)
			{
				code.AppendFormat(@"
			case {4}{0}{4}:
				{1} = __ext as {2};
				break;{3}", ext.ExtensionName, ext.ExtensionName, ext.ExtensionClassName, Environment.NewLine, '"');
			}
			code.AppendLine(@"
		}
	}");

			code.AppendLine(@"
	static IMessage fakeMsg = new Content(0, null, new MessageTimestamp(), StringSlice.Empty, SeverityFlag.Info);
			");

			code.AppendLine(@"
	public override LogJoint.IMessage MakeMessage(LogJoint.IMessagesBuilderCallback __callback,
		LogJoint.MakeMessageFlags __flags)
	{
			");

			bool timeAdded = false;
			bool bodyAdded = false;
			bool threadAdded = false;
			bool fieldsAdded = false;
			bool severityAdded = false;
			bool typeAdded = false;

			string defTimeExpression = @"DateTime.MinValue";

			string defBodyExpression = @"StringSlice.Empty";

			string defThreadExpression = @"StringSlice.Empty";

			string defSeverityExpression = @"Severity.Info";

			string defTypeExpression = @"EntryType.Content";

			foreach (OutputFieldStruct s in outputFields)
			{
				string fieldVar = null;
				string fieldType = null;
				string ignoranceFlag = null;
				string exprWhenIgnored = null;
				switch (s.Name)
				{
					case "Time":
						fieldVar = "__time";
						fieldType = "DateTime";
						ignoranceFlag = "HintIgnoreTime";
						exprWhenIgnored = defTimeExpression;
						timeAdded = true;
						break;
					case "Body":
						fieldVar = "__body";
						fieldType = "StringSlice";
						ignoranceFlag = "HintIgnoreBody";
						exprWhenIgnored = defBodyExpression;
						bodyAdded = true;
						break;
					case "Thread":
						fieldVar = "__thread";
						fieldType = "StringSlice";
						ignoranceFlag = "HintIgnoreThread";
						exprWhenIgnored = defThreadExpression;
						threadAdded = true;
						break;
					case "Severity":
						fieldVar = "__severity";
						fieldType = "Severity";
						severityAdded = true;
						ignoranceFlag = "HintIgnoreSeverity";
						exprWhenIgnored = defSeverityExpression;
						break;
					case "EntryType":
						fieldVar = "__entryType";
						fieldType = "EntryType";
						ignoranceFlag = "HintIgnoreEntryType";
						exprWhenIgnored = defTypeExpression;
						typeAdded = true;
						break;
				}
				if (fieldVar != null)
				{
					code.AppendFormat(@"
		{0} {1};
		if ((__flags & LogJoint.MakeMessageFlags.{2}) == 0)
			{1} = {3};
		else 
			{1} = {4};",
					fieldType, fieldVar, ignoranceFlag,
					GetOutputFieldExpression(s, fieldType, helperFunctions),
					exprWhenIgnored);
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
				code.AppendFormat(@"
		DateTime __time = {0};", defTimeExpression);
			}

			if (!bodyAdded)
			{
				code.AppendFormat(@"
		StringSlice __body = {0};", defBodyExpression);
			}

			if (!threadAdded)
			{
				code.AppendFormat(@"
		StringSlice __thread = {0};", defThreadExpression);
			}

			if (!severityAdded)
			{
				code.AppendFormat(@"
		Severity __severity = {0};", defSeverityExpression);
			}

			if (!typeAdded)
			{
				code.AppendFormat(@"
		EntryType __entryType = {0};", defTypeExpression);
			}

			code.AppendLine(@"
		if ((__flags & LogJoint.MakeMessageFlags.HintIgnoreBody) == 0)
			__body = TRIM(__body);");


			//            if (fieldsAdded)
			//            {
			//                code.AppendLine(@"
			//		if ((__flags & LogJoint.MakeMessageFlags.HintIgnoreBody) == 0)
			//			__body += __fields.ToString();");
			//            }

			code.AppendLine(@"
		LogJoint.IThread mtd = __callback.GetThread(__thread);

		__time = __ApplyTimeOffset(__time);

		//fakeMsg.SetPosition(__callback.CurrentPosition);
		//return fakeMsg;

		switch (__entryType)
		{
		case EntryType.FrameBegin:
			return new FrameBegin(
				__callback.CurrentPosition,
				mtd, 
				new MessageTimestamp(__time), 
				__body);
		case EntryType.FrameEnd:
			return new FrameEnd(
				__callback.CurrentPosition,
				mtd, 
				new MessageTimestamp(__time));
		default:
			return new Content(
				__callback.CurrentPosition,
				mtd,
				new MessageTimestamp(__time),
				__body,
				(SeverityFlag)__severity
			);
		}");

			code.AppendLine(@"
	}");

			code.AppendLine(@"
	public override LogJoint.Internal.__MessageBuilder Clone()
	{
		return new GeneratedMessageBuilder();
	}
");

			code.Append(helperFunctions.ToString());

			code.AppendLine(@"
}");
			return code.ToString();
		}

		private static void ThrowBadUserCodeException(string fullCode, CompilerResults cr)
		{
			StringBuilder exceptionMessage = new StringBuilder();
			StringBuilder allErrors = new StringBuilder();
			BadUserCodeException.BadFieldDescription badField = null;
			string errorMessage = null;

			exceptionMessage.Append("Failed to process log fields. There must be an error in format's configuration. ");
	
			foreach (CompilerError err in cr.Errors)
			{
				if (err.IsWarning)
					continue;

				exceptionMessage.AppendLine(err.ErrorText);
				allErrors.AppendFormat("Line {0} Column {1}: ({2}) {3}{4}", 
					err.Line, err.Column, err.ErrorNumber, err.ErrorText, Environment.NewLine);
				errorMessage = err.ErrorText;

				int globalErrorPos;
				UserCode.Entry? userCodeEntry;
				UserCode.FindErrorLocation(fullCode, err, out globalErrorPos, out userCodeEntry);
				if (userCodeEntry != null)
				{
					badField = new BadUserCodeException.BadFieldDescription(
						userCodeEntry.Value.FieldName, globalErrorPos - userCodeEntry.Value.Index);
				}

				break;
			}

			throw new BadUserCodeException(exceptionMessage.ToString(), fullCode, errorMessage, allErrors.ToString(), badField);
		}
#endif

		Internal.__MessageBuilder builder;

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

		class CodedomEnvironmentConfigurator: IDisposable
		{
#if MONOMAC
			string monoEnvOptions;

			public CodedomEnvironmentConfigurator()
			{
				Directory.SetCurrentDirectory(@"/Library/Frameworks/Mono.framework/Versions/Current/bin");
				monoEnvOptions = Environment.GetEnvironmentVariable("MONO_ENV_OPTIONS");
				Environment.SetEnvironmentVariable("MONO_ENV_OPTIONS", "");
			}

			void IDisposable.Dispose()
			{
				if (!string.IsNullOrEmpty(monoEnvOptions))
					Environment.SetEnvironmentVariable("MONO_ENV_OPTIONS", monoEnvOptions);
			}
#else
			void IDisposable.Dispose()
			{
			}
#endif
		};

		readonly List<string> inputFieldNames;
		readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
		OutputFieldStruct timeField;
		readonly List<ExtensionInfo> extensions = new List<ExtensionInfo>();
		Type precompiledBuilderType;

		#endregion
	};
}
