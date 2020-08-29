using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.FieldsProcessor
{
	public interface IMessagesBuilderCallback
	{
		long CurrentPosition { get; }
		long CurrentEndPosition { get; }
		StringSlice CurrentRawText { get; }
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

	public interface IFieldsProcessor
	{
		void Reset();
		void SetSourceTime(DateTime sourceTime);
		void SetPosition(long value);
		void SetTimeOffsets(ITimeOffsets value);
		void SetInputField(int idx, StringSlice value);
		IMessage MakeMessage(IMessagesBuilderCallback callback, MakeMessageFlags flags);
		bool IsBodySingleFieldExpression();
	};

	public interface IMetadataReferencesProvider
    {
		IReadOnlyList<Microsoft.CodeAnalysis.MetadataReference> GetMetadataReferences(IEnumerable<string> assemblyNames);
	};

	public interface IAssemblyLoader
	{
		System.Reflection.Assembly Load(byte[] image);
	};

	public interface IInitializationParams
	{
	}

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

	public interface IFactory
	{
		IInitializationParams CreateInitializationParams(
			XElement fieldsNode, bool performChecks
		);
		IFieldsProcessor CreateProcessor(
			IInitializationParams initializationParams,
			IEnumerable<string> inputFieldNames,
			IEnumerable<ExtensionInfo> extensions,
			LJTraceSource trace
		);
	};
}
