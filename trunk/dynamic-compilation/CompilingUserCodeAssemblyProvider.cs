using LogJoint.FieldsProcessor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class CompilingUserCodeAssemblyProvider: IUserCodeAssemblyProvider
	{
		readonly IMetadataReferencesProvider metadataReferencesProvider;
		Extensibility.IPluginsManagerInternal pluginsManager;


		public CompilingUserCodeAssemblyProvider(
			IMetadataReferencesProvider metadataReferencesProvider
		)
		{
			this.metadataReferencesProvider = metadataReferencesProvider;
		}

		void IUserCodeAssemblyProvider.SetPluginsManager(Extensibility.IPluginsManagerInternal pluginsManager)
		{
			this.pluginsManager = pluginsManager;
		}

		int IUserCodeAssemblyProvider.ProviderVersionHash => Hashing.GetStableHashCode(
				typeof(CSharpCompilation).Assembly.FullName + " gen=1");

		byte[] IUserCodeAssemblyProvider.GetUserCodeAsssembly(
			LJTraceSource trace, 
			List<string> inputFieldNames, 
			List<ExtensionInfo> extensions, 
			List<OutputFieldStruct> outputFields)
		{
			using var perfop = new Profiling.Operation(trace, "compile user code");
			string fullCode = MakeMessagesBuilderCode(inputFieldNames, extensions, outputFields);

			var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

			var metadataReferences = new List<MetadataReference>(metadataReferencesProvider.GetMetadataReferences());

			foreach (var ext in extensions)
			{
				var asmName = $"{new AssemblyName(ext.ExtensionAssemblyName).Name}.dll";
				var asmFile = pluginsManager.InstalledPlugins.SelectMany(
					p => p.Files).FirstOrDefault(f => f.RelativePath == asmName);
				if (asmFile == null)
					throw new Exception($"Display extension assembly {ext.ExtensionAssemblyName} can not be found");
				metadataReferences.Add(MetadataReference.CreateFromFile(asmFile.AbsolutePath));
			}

			CSharpCompilation compilation = CSharpCompilation.Create(
				$"UserCode{Guid.NewGuid():N}",
				new[] { syntaxTree },
				metadataReferences,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
					.WithOptimizationLevel(OptimizationLevel.Release)
			);

			using var dllStream = new MemoryStream();
			perfop.Milestone("started compile");
			var emitResult = compilation.Emit(dllStream);
			if (!emitResult.Success)
			{
				ThrowBadUserCodeException(fullCode, emitResult.Diagnostics);
			}
			dllStream.Flush();
			dllStream.Position = 0;
			perfop.Milestone("getting type");
			var rawAsm = dllStream.ToArray();
			return rawAsm;
		}

		static string MakeMessagesBuilderCode(List<string> inputFieldNames,
			List<ExtensionInfo> extensions, List<OutputFieldStruct> outputFields)
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
	public override LogJoint.IMessage MakeMessage(LogJoint.FieldsProcessor.IMessagesBuilderCallback __callback,
		LogJoint.FieldsProcessor.MakeMessageFlags __flags)
	{
			");

			bool timeAdded = false;
			bool bodyAdded = false;
			bool threadAdded = false;
			bool fieldsAdded = false;
			bool severityAdded = false;
			bool typeAdded = false;
			bool linkAdded = false;

			string defTimeExpression = @"DateTime.MinValue";

			string defBodyExpression = @"StringSlice.Empty";

			string defThreadExpression = @"StringSlice.Empty";

			string defSeverityExpression = @"Severity.Info";

			string defTypeExpression = @"EntryType.Content";

			string defLinkExpression = @"StringSlice.Empty";

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
					case "Link":
						fieldVar = "__link";
						fieldType = "StringSlice";
						ignoranceFlag = "HintIgnoreLink";
						exprWhenIgnored = defLinkExpression;
						linkAdded = true;
						break;
				}
				if (fieldVar != null)
				{
					code.AppendFormat(@"
		{0} {1};
		if ((__flags & LogJoint.FieldsProcessor.MakeMessageFlags.{2}) == 0)
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

			if (!linkAdded)
			{
				code.AppendFormat(@"
		StringSlice __link = {0};", defLinkExpression);
			}

			code.AppendLine(@"
		if ((__flags & LogJoint.FieldsProcessor.MakeMessageFlags.HintIgnoreBody) == 0)
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

		var __result = new Message(
			__callback.CurrentPosition,
			__callback.CurrentEndPosition,
			mtd,
			new MessageTimestamp(__time),
			__body,
			(SeverityFlag)__severity,
			__callback.CurrentRawText
		);

		if (!__link.IsEmpty)
			__result.SetLink(__link);

		return __result;
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

		static string GetOutputFieldExpression(OutputFieldStruct s, string type, StringBuilder helperFunctions)
		{
			bool retTypeIsStringSlice = type == "StringSlice";
			switch (s.Type)
			{
				case OutputFieldStruct.CodeType.Expression:
					var fmt = retTypeIsStringSlice ? "new StringSlice({0}{2}{1})" : "{0}{2}{1}";
					return string.Format(fmt, UserCode.GetProlog(s.Name), UserCode.GetEpilog(), s.Code);
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
					UserCode.GetProlog(s.Name), UserCode.GetEpilog());
					return string.Format(helperCallFmt, helperFuncName);
				default:
					Debug.Assert(false);
					return "";
			}
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

				UserCode.FindErrorLocation(fullCode, err, out int globalErrorPos, out UserCode.Entry? userCodeEntry);
				if (userCodeEntry != null)
				{
					badField = new BadUserCodeException.BadFieldDescription(
						userCodeEntry.Value.FieldName, globalErrorPos - userCodeEntry.Value.Index);
				}

				break;
			}

			throw new BadUserCodeException(exceptionMessage.ToString(), fullCode, errorMessage, allErrors.ToString(), badField);
		}

		static readonly Regex escapeFieldNameRe = new Regex(@"[^\w]");

		static string EscapeFieldName(string name)
		{
			return escapeFieldNameRe.Replace(name, "_");
		}

		static class UserCode
		{
			public static string GetProlog(string fieldName)
			{
				return string.Format("/* User code begin. Field: {0} */ ", fieldName);
			}

			public static string GetEpilog()
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

			public static void FindErrorLocation(string code, Diagnostic error, out int globalErrorPos, out UserCode.Entry? userCodeEntry)
			{
				globalErrorPos = 0;
				userCodeEntry = null;

				if (!error.Location.IsInSource)
					return;
				var lineSpan = error.Location.GetLineSpan();
				if (!lineSpan.IsValid)
					return;

				List<UserCode.LineInfo> lines = new List<UserCode.LineInfo>(UserCode.GetLines(code));

				int lineIdx = lineSpan.EndLinePosition.Line;
				if (lineIdx < 0 || lineIdx >= lines.Count)
					return;

				globalErrorPos = lines[lineIdx].Position + lineSpan.EndLinePosition.Character;
				foreach (UserCode.Entry uce in UserCode.GetEntries(code))
				{
					if (globalErrorPos >= uce.Index && globalErrorPos < (uce.Index + uce.Length))
					{
						userCodeEntry = uce;
						break;
					}
				}
			}
		}
	}
}
