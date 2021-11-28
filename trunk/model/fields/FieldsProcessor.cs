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
				if (performChecks)
				{
					if (this.timeField.Name == null)
						throw new Exception("'Time' field is not defined");
				}
			}

			internal void InitializeInstance(FieldsProcessorImpl proc)
			{
				proc.outputFields.AddRange(outputFields);
			}

			readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
			readonly OutputFieldStruct timeField;
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
				var processor = new FieldsProcessorImpl(
					(InitializationParams)initializationParams,
					inputFieldNames,
					extensions,
					await cacheEntryTask,
					trace,
					telemetryCollector,
					userCodeAssemblyProvider,
					assemblyLoader
				);
				await processor.Init();
				return processor;
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
			InitializationParams initializationParams, 
			IEnumerable<string> inputFieldNames, 
			IEnumerable<ExtensionInfo> extensions,
			Persistence.IStorageEntry cacheEntry,
			LJTraceSource trace,
			Telemetry.ITelemetryCollector telemetryCollector,
			IUserCodeAssemblyProvider userCodeAssemblyProvider,
			IAssemblyLoader assemblyLoader
		)
		{
			if (inputFieldNames == null)
				throw new ArgumentNullException(nameof (inputFieldNames));
			initializationParams.InitializeInstance(this);
			if (extensions != null)
				this.extensions.AddRange(extensions);
			this.inputFieldNames = inputFieldNames.Select((name, idx) => name ?? string.Format("Field{0}", idx)).ToList();
			this.cacheEntry = cacheEntry;
			this.trace = trace;
			this.telemetryCollector = telemetryCollector;
			this.userCodeAssemblyProvider = userCodeAssemblyProvider;
			this.assemblyLoader = assemblyLoader;
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
			await using var cacheSection = await cacheEntry.OpenRawStreamSection($"builder-code-{builderTypeHash}",
				Persistence.StorageSectionOpenFlag.ReadWrite);
			var cachedRawAsmSize = cacheSection.Data.Length;
			trace.Info("Type hash: {0}. Cache size: {1}", builderTypeHash, cachedRawAsmSize);
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
			byte[] rawAsm = userCodeAssemblyProvider.GetUserCodeAsssembly(
				trace, inputFieldNames, extensions, outputFields);
			Assembly asm = Assembly.Load(rawAsm);
			cacheSection.Data.Position = 0;
			await cacheSection.Data.WriteAsync(rawAsm, 0, rawAsm.Length);
			return asm.GetType("GeneratedMessageBuilder");
		}


		Internal.__MessageBuilder builder;

		readonly List<string> inputFieldNames;
		readonly List<OutputFieldStruct> outputFields = new List<OutputFieldStruct>();
		readonly List<ExtensionInfo> extensions = new List<ExtensionInfo>();
		readonly Persistence.IStorageEntry cacheEntry;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly LJTraceSource trace;
		readonly IUserCodeAssemblyProvider userCodeAssemblyProvider;
		readonly IAssemblyLoader assemblyLoader;

		#endregion
	};
}
