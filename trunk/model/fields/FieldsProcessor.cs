using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace LogJoint
{
	public partial class FieldsProcessor: IFieldsProcessor
	{
		public class InitializationParams
		{
			public InitializationParams(XElement fieldsNode, bool performChecks, Type precompiledUserCode)
			{
				if (fieldsNode == null)
					throw new ArgumentNullException(nameof (fieldsNode));
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
					throw new ArgumentException(nameof(extensionName));
				if (string.IsNullOrEmpty(extensionAssemblyName))
					throw new ArgumentException(nameof(extensionAssemblyName));
				if (string.IsNullOrEmpty(extensionClassName))
					throw new ArgumentException(nameof(extensionClassName));
				if (instanceGetter == null)
					throw new ArgumentNullException(nameof (instanceGetter));
				if (!StringUtils.IsValidCSharpIdentifier(extensionName))
					throw new ArgumentException("extensionName must be a valid C# identifier", nameof (extensionName));

				this.ExtensionName = extensionName;
				this.ExtensionAssemblyName = extensionAssemblyName;
				this.ExtensionClassName = extensionClassName;
				this.InstanceGetter = instanceGetter;
			}
		};

		public class Factory : IFieldsProcessorFactory
		{
			readonly ITempFilesManager tempFilesManager;
			readonly Persistence.IStorageEntry cacheEntry;

			public Factory(
				ITempFilesManager tempFilesManager,
				Persistence.IStorageManager storageManager
			)
			{
				this.tempFilesManager = tempFilesManager;
				this.cacheEntry = storageManager.GetEntry("user-code-cache", 0x81012231);
			}

			IFieldsProcessor IFieldsProcessorFactory.Create(
				InitializationParams initializationParams,
				IEnumerable<string> inputFieldNames,
				IEnumerable<ExtensionInfo> extensions,
				LJTraceSource trace)
			{
				return new FieldsProcessor(
					initializationParams,
					inputFieldNames,
					extensions,
					tempFilesManager,
					cacheEntry,
					trace
				);
			}
		};

		public FieldsProcessor(
			InitializationParams initializationParams, 
			IEnumerable<string> inputFieldNames, 
			IEnumerable<ExtensionInfo> extensions,
			ITempFilesManager tempFilesManager,
			Persistence.IStorageEntry cacheEntry,
			LJTraceSource trace)
		{
			if (inputFieldNames == null)
				throw new ArgumentNullException(nameof (inputFieldNames));
			initializationParams.InitializeInstance(this);
			if (extensions != null)
				this.extensions.AddRange(extensions);
			this.inputFieldNames = inputFieldNames.Select((name, idx) => name ?? string.Format("Field{0}", idx)).ToList();
			this.tempFilesManager = tempFilesManager;
			this.cacheEntry = cacheEntry;
			this.trace = trace;
		}

		void IFieldsProcessor.Reset()
		{
			if (builder == null)
				builder = CreateBuilderInstance();

			builder.ResetFieldValues();
			builder.__sourceTime = new DateTime();
			builder.__position = 0;
			builder.__timeOffsets = TimeOffsets.Empty;
		}

		void IFieldsProcessor.SetSourceTime(DateTime sourceTime)
		{
			builder.__sourceTime = sourceTime;
		}

		void IFieldsProcessor.SetPosition(long value)
		{
			builder.__position = value;
		}

		void IFieldsProcessor.SetTimeOffsets(ITimeOffsets value)
		{
			builder.__timeOffsets = value;
		}

		void IFieldsProcessor.SetInputField(int idx, StringSlice value)
		{
			builder.SetInputFieldByIndex(idx, value);
		}

		IMessage IFieldsProcessor.MakeMessage(IMessagesBuilderCallback callback, MakeMessageFlags flags)
		{
			return builder.MakeMessage(callback, flags);
		}

		bool IFieldsProcessor.IsBodySingleFieldExpression()
		{
			var bodyFld = outputFields.FirstOrDefault(f => f.Name == "Body");
			if (bodyFld.Name == null)
				return false;
			return 
				bodyFld.Type == OutputFieldStruct.CodeType.Expression
			 && inputFieldNames.Contains(bodyFld.Code);
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
		int GetMessageBuilderTypeHash()
		{
			int typeHash = Hashing.GetHashCode(0);
			typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(
				typeof(CSharpCompilation).Assembly.FullName));
			foreach (string i in inputFieldNames)
			{
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i));
			}
			foreach (OutputFieldStruct i in outputFields)
			{
				typeHash = Hashing.GetHashCode(typeHash, (int)i.Type);
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i.Name));
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i.Code));
			}
			foreach (ExtensionInfo i in extensions)
			{
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i.ExtensionAssemblyName));
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i.ExtensionClassName));
				typeHash = Hashing.GetHashCode(typeHash, Hashing.GetStableHashCode(i.ExtensionName));
			}
			return typeHash;
		}

		Internal.__MessageBuilder CreateBuilderInstance()
		{
			Type builderType = precompiledBuilderType;

			if (builderType == null)
			{
				int builderTypeHash = GetMessageBuilderTypeHash();

				Task<Type> builderTypeTask;
				lock (builderTypesCache)
				{
					if (!builderTypesCache.TryGetValue(builderTypeHash, out builderTypeTask))
					{
						builderTypeTask = Task.Run(() => 
						{
							using (var cacheESection = cacheEntry.OpenRawStreamSection("code",
								Persistence.StorageSectionOpenFlag.ReadOnly, additionalNumericKey: builderTypeHash))
							{
								// cacheESection.
							}
							return CompileUserCodeToTypeInternal(
								asmName => Assembly.Load(asmName).Location);
						});
						builderTypesCache.Add(builderTypeHash, builderTypeTask);
					}
				}

				try
				{
					builderType = builderTypeTask.Result;
				}
				catch (AggregateException e)
				{
					throw e.InnerException;
				}
			}

			Internal.__MessageBuilder ret = (Internal.__MessageBuilder)Activator.CreateInstance(builderType);

			foreach (ExtensionInfo ext in extensions)
				ret.SetExtensionByName(ext.ExtensionName, ext.InstanceGetter());

			return ret;
		}

		static Dictionary<int, Task<Type>> builderTypesCache = new Dictionary<int, Task<Type>>();

		Type CompileUserCodeToTypeInternal(Func<string, string> assemblyLocationResolver)
		{
			using (var perfop = new Profiling.Operation(trace, "compile user code"))
			{
				List<string> refs = new List<string>();
				string fullCode = MakeMessagesBuilderCode(inputFieldNames, extensions, outputFields, refs);

				var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

				var metadataReferences = new List<MetadataReference>();
				metadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
				metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocationResolver("System.Runtime")));
				metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocationResolver("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")));
				metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocationResolver(Assembly.GetExecutingAssembly().FullName)));
				metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocationResolver(typeof(StringSlice).Assembly.FullName)));
				metadataReferences.AddRange(refs.Select(refAsm => MetadataReference.CreateFromFile(assemblyLocationResolver(refAsm))));

				CSharpCompilation compilation = CSharpCompilation.Create(
					$"UserCode{Guid.NewGuid().ToString("N")}",
					new[] { syntaxTree },
					metadataReferences,
					new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
						.WithOptimizationLevel(OptimizationLevel.Release)
				);

				using (var dllStream = new MemoryStream())
				{
					perfop.Milestone("started compile");
					var emitResult = compilation.Emit(dllStream);
					if (!emitResult.Success)
					{
						ThrowBadUserCodeException(fullCode, emitResult.Diagnostics);
					}
					dllStream.Flush();
					dllStream.Position = 0;
					perfop.Milestone("getting type");
					var asm = Assembly.Load(dllStream.ToArray());
					return asm.GetType("GeneratedMessageBuilder");
				}
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
	static IMessage fakeMsg = new Message(0, 0, null, new MessageTimestamp(), StringSlice.Empty, SeverityFlag.Info);
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
					if (!fieldsAdded) // todo: remove __fields
					{
						code.AppendLine(@"
		StringBuilder __fields = new StringBuilder();
		");
						fieldsAdded = true;
					}
					code.AppendFormat(@"
		__fields.AppendLine();
		__fields.AppendFormat(""{{0}}={{1}}"", ""{0}"", {1});",
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

		return new Message(
			__callback.CurrentPosition,
			__callback.CurrentEndPosition,
			mtd,
			new MessageTimestamp(__time),
			__body,
			(SeverityFlag)__severity,
			__callback.CurrentRawText
		);
		");

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

		private static void ThrowBadUserCodeException(string fullCode, ImmutableArray<Diagnostic> cr)
		{
			StringBuilder exceptionMessage = new StringBuilder();
			StringBuilder allErrors = new StringBuilder();
			BadUserCodeException.BadFieldDescription badField = null;
			string errorMessage = null;

			exceptionMessage.Append("Failed to process log fields. There must be an error in format's configuration. ");
	
			foreach (Diagnostic err in cr)
			{
				if (err.DefaultSeverity != DiagnosticSeverity.Error)
					continue;
				

				exceptionMessage.AppendLine(err.GetMessage());
				if (err.Location.IsInSource && err.Location.GetLineSpan().IsValid)
					allErrors.AppendFormat("Line {0} Column {1}: {2}{3}",
						err.Location.GetLineSpan().EndLinePosition.Line, err.Location.GetLineSpan().EndLinePosition.Character,
						err.GetMessage(), err.GetMessage(), Environment.NewLine);
				errorMessage = err.GetMessage();

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

		readonly List<string> inputFieldNames;
		readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
		readonly List<ExtensionInfo> extensions = new List<ExtensionInfo>();
		readonly ITempFilesManager tempFilesManager;
		readonly Persistence.IStorageEntry cacheEntry;
		readonly LJTraceSource trace;
		Type precompiledBuilderType;

		#endregion
	};
}
