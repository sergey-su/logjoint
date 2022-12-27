using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace LogJoint.FieldsProcessor
{
	public partial class FieldsProcessorImpl: IFieldsProcessor
	{
		public class InitializationParams: IInitializationParams
		{
			public InitializationParams(XElement fieldsNode, bool performChecks)
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
				var precompiledElement = fieldsNode.Element("precompiled");
				if (precompiledElement != null)
				{
					precompiledAssmebly = Convert.FromBase64String(precompiledElement.Value);
				}
				if (performChecks)
				{
					if (this.timeField.Name == null)
						throw new Exception("'Time' field is not defined");
				}
			}

			internal IEnumerable<OutputFieldStruct> OutputFields => outputFields;
			internal byte[] PrecompiledAssmebly => precompiledAssmebly;

			readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
			readonly OutputFieldStruct timeField;
			readonly byte[] precompiledAssmebly;
		};

		public class Factory : IFactory
		{
			readonly Task<Persistence.IStorageEntry> cacheEntryTask;
			readonly Telemetry.ITelemetryCollector telemetryCollector;
			readonly IUserCodeAssemblyProvider userCodeAssemblyProvider;
			readonly IAssemblyLoader assemblyLoader;

			public Factory(
				Persistence.IStorageManager storageManager,
				Telemetry.ITelemetryCollector telemetryCollector,
				IUserCodeAssemblyProvider userCodeAssemblyProvider,
				IAssemblyLoader assemblyLoader
			)
			{
				this.cacheEntryTask = storageManager.GetEntry("user-code-cache", 0x81012232);
				this.telemetryCollector = telemetryCollector;
				this.userCodeAssemblyProvider = userCodeAssemblyProvider;
				this.assemblyLoader = assemblyLoader ?? new DefaultAssemblyLoader();
			}

			IInitializationParams IFactory.CreateInitializationParams(
				XElement fieldsNode, bool performChecks
			) => new InitializationParams(fieldsNode, performChecks);

			async ValueTask<IFieldsProcessor> IFactory.CreateProcessor(
				IInitializationParams initializationParams,
				IEnumerable<string> inputFieldNames,
				IEnumerable<ExtensionInfo> extensions,
				LJTraceSource trace)
			{
				var initParams = (InitializationParams)initializationParams;
				var processor = new FieldsProcessorImpl(
					SanitizeInputFieldNames(inputFieldNames),
					initParams.OutputFields,
					SanitizeExtensions(extensions),
					await cacheEntryTask,
					trace,
					telemetryCollector,
					userCodeAssemblyProvider,
					initParams.PrecompiledAssmebly,
					assemblyLoader
				);
				await processor.Init();
				return processor;
			}

			byte[] IFactory.CreatePrecompiledAssembly(
				IInitializationParams initializationParams,
				IEnumerable<string> inputFieldNames,
				IEnumerable<ExtensionInfo> extensions,
				string assemblyName,
				LJTraceSource trace
			)
			{
				var initParams = (InitializationParams)initializationParams;
				return userCodeAssemblyProvider.GetUserCodeAsssembly(
					trace, SanitizeInputFieldNames(inputFieldNames).ToList(),
					SanitizeExtensions(extensions).ToList(), initParams.OutputFields.ToList(), assemblyName);
			}

			static private IEnumerable<string> SanitizeInputFieldNames(IEnumerable<string> inputFieldNames)
			{
				if (inputFieldNames == null)
					throw new ArgumentNullException(nameof(inputFieldNames));
				return inputFieldNames.Select((name, idx) => name ?? string.Format("Field{0}", idx));
			}

			static private IEnumerable<ExtensionInfo> SanitizeExtensions(IEnumerable<ExtensionInfo> extensions)
			{
				return extensions ?? new ExtensionInfo[0];
			}
		};

		private class DefaultAssemblyLoader: IAssemblyLoader
		{
			Assembly IAssemblyLoader.Load(byte[] image)
			{
				return Assembly.Load(image);
			}
		};

		public FieldsProcessorImpl(
			IEnumerable<string> inputFieldNames,
			IEnumerable<OutputFieldStruct> outputFields,
			IEnumerable<ExtensionInfo> extensions,
			Persistence.IStorageEntry cacheEntry,
			LJTraceSource trace,
			Telemetry.ITelemetryCollector telemetryCollector,
			IUserCodeAssemblyProvider userCodeAssemblyProvider,
			byte[] precompiledAssembly,
			IAssemblyLoader assemblyLoader
		)
		{
			this.inputFieldNames = inputFieldNames.ToList();
			this.outputFields = outputFields.ToList();
			this.extensions = extensions.ToList();
			this.cacheEntry = cacheEntry;
			this.trace = trace;
			this.telemetryCollector = telemetryCollector;
			this.userCodeAssemblyProvider = userCodeAssemblyProvider;
			this.assemblyLoader = assemblyLoader;
			this.precompiledAssembly = precompiledAssembly;
		}

		public async Task Init()
		{
			builder = await CreateBuilderInstance();
		}

		void IFieldsProcessor.Reset()
		{
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

		/// <summary>
		/// Calculates an integer hash out of all fields that the message builder type depends on
		/// </summary>
		int GetMessageBuilderTypeHash()
		{
			int typeHash = Hashing.GetHashCode(0);
			if (userCodeAssemblyProvider != null)
				typeHash = Hashing.GetHashCode(typeHash, userCodeAssemblyProvider.ProviderVersionHash);
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

		async ValueTask<Internal.__MessageBuilder> CreateBuilderInstance()
		{
			int builderTypeHash = GetMessageBuilderTypeHash();
			Task<Type> builderTypeTask;
			lock (builderTypesCache)
			{
				if (!builderTypesCache.TryGetValue(builderTypeHash, out builderTypeTask))
				{
					builderTypeTask = GenerateType(builderTypeHash);
					builderTypesCache.Add(builderTypeHash, builderTypeTask);
				}
			}

			var builderType = await builderTypeTask;

			Internal.__MessageBuilder ret = (Internal.__MessageBuilder)Activator.CreateInstance(builderType);

			Assembly dependencyResolveHandler(object s, ResolveEventArgs e)
			{
				var name = (new AssemblyName(e.Name)).Name;
				var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
				return asm;
			}
			AppDomain.CurrentDomain.AssemblyResolve += dependencyResolveHandler;
			try
			{
				foreach (ExtensionInfo ext in extensions)
					ret.SetExtensionByName(ext.ExtensionName, ext.InstanceGetter());
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= dependencyResolveHandler;
			}

			return ret;
		}

		static Dictionary<int, Task<Type>> builderTypesCache = new Dictionary<int, Task<Type>>();

		async Task<Type> GenerateType(int builderTypeHash)
		{
			if (precompiledAssembly != null && userCodeAssemblyProvider == null)
			{
				return assemblyLoader.Load(precompiledAssembly).GetType("GeneratedMessageBuilder");
			}
			await using var cacheSection = await cacheEntry.OpenRawStreamSection($"builder-code-{builderTypeHash}",
				Persistence.StorageSectionOpenFlag.ReadWrite);
			var cachedRawAsmSize = cacheSection.Data.Length;
			trace.Info("Type hash: {0}. Cache size: {1}. Precompiled size: {2}",
				builderTypeHash, cachedRawAsmSize, precompiledAssembly?.Length);
			if (cachedRawAsmSize > 0)
			{
				try
				{
					var cachedRawAsm = new byte[cachedRawAsmSize];
					await cacheSection.Data.ReadAsync(cachedRawAsm, 0, (int)cachedRawAsmSize);
					return assemblyLoader.Load(cachedRawAsm).GetType("GeneratedMessageBuilder");
				}
				catch (Exception e)
				{
					trace.Error(e, "Failed to load cached builder");
					telemetryCollector.ReportException(e, "loading cached builder asm");
				}
			}
			if (userCodeAssemblyProvider == null)
			{
				throw new Exception("User code is not precompiled and no provider is given");
			}
			byte[] rawAsm = userCodeAssemblyProvider.GetUserCodeAsssembly(
				trace, inputFieldNames, extensions, outputFields, assemblyName: null);
			Assembly asm = Assembly.Load(rawAsm);
			cacheSection.Data.Position = 0;
			await cacheSection.Data.WriteAsync(rawAsm, 0, rawAsm.Length);
			return asm.GetType("GeneratedMessageBuilder");
		}


		Internal.__MessageBuilder builder;

		readonly List<string> inputFieldNames;
		readonly List<OutputFieldStruct> outputFields;
		readonly List<ExtensionInfo> extensions;
		readonly Persistence.IStorageEntry cacheEntry;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly LJTraceSource trace;
		readonly IUserCodeAssemblyProvider userCodeAssemblyProvider;
		readonly IAssemblyLoader assemblyLoader;
		readonly byte[] precompiledAssembly;

		#endregion
	};
}
