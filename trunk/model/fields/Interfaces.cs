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

	public interface IFieldsProcessorFactory
	{
		IFieldsProcessor Create(
			FieldsProcessor.InitializationParams initializationParams,
			IEnumerable<string> inputFieldNames,
			IEnumerable<FieldsProcessor.ExtensionInfo> extensions,
			LJTraceSource trace
		);
	};
}
