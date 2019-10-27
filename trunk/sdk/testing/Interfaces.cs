using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	public interface IContext
	{
		IModel Model { get; }
		UI.Presenters.IPresentation Presentation { get; }
		IMocks Mocks { get; }
		IRegistry Registry { get; }
		ISamples Samples { get; }
		IUtils Utils { get; }
		string AppDataDirectory { get; }
	};

	public interface IMocks
	{
		UI.Presenters.IPromptDialog PromptDialog { get; }
		UI.Presenters.IClipboardAccess ClipboardAccess { get; }
	};

	public interface IRegistry
	{
		void Set<T>(T value);
		T Get<T>();
	};

	public interface ISamples
	{
		Task<string> GetSampleAsLocalFile(string sampleName);
		Task<Stream> GetSampleAsStream(string sampleName);
		Uri GetSampleAsUri(string sampleName);
	};

	public interface IUtils
	{
		Task EmulateFileDragAndDrop(string filePath);
		Task WaitFor(Func<bool> condition, string operationName = null, TimeSpan? timeout = null);
	};
}