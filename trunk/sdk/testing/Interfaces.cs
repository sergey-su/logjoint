using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	/// <summary>
	/// Stores the context of the individual integration test.
	/// It provides access to the API of the headless logjoint instance
	/// created for this test.
	/// It also provides access to different test helpers.
	/// </summary>
	public interface IContext
	{
		/// <summary>
		/// Model-layer API of the headless logjoint instance.
		/// </summary>
		IModel Model { get; }
		/// <summary>
		/// Presentation-layer API of the headless logjoint instance.
		/// </summary>
		UI.Presenters.IPresentation Presentation { get; }
		/// <summary>
		/// Gets object with references to NSubstitute mocks that
		/// the headless logjoint instance is wired with.
		/// </summary>
		IMocks Mocks { get; }
		/// <summary>
		/// Gets the reference to general-purpose registry that
		/// plugin's test entry point can use to store information
		/// for later use in the test cases. For example, plugin entry point
		/// can store a reference to mocks it has created.
		/// </summary>
		IRegistry Registry { get; }
		/// <summary>
		/// Gets the reference to the utility object that manages
		/// test data samples, usually logs.
		/// </summary>
		ISamples Samples { get; }
		/// <summary>
		/// Gets the reference to object that implements SDK test helpers.
		/// </summary>
		IUtils Utils { get; }
		/// <summary>
		/// Directory where the headless logjoint instance is configured to
		/// store its data. Internal logjoint logs are stored there too.
		/// </summary>
		string AppDataDirectory { get; }
	};

	/// <summary>
	/// A collection of some NSubstitute mocks that logjoint instance is wired with.
	/// In the headless logjoint instance UI and platform-dependent
	/// objects are mocked.
	/// </summary>
	public interface IMocks
	{
		/// <summary>
		/// NSubstitute mock of prompt dialog UI.
		/// </summary>
		UI.Presenters.IPromptDialog PromptDialog { get; }
		/// <summary>
		/// NSubstitute mock of clipboard interface.
		/// </summary>
		UI.Presenters.IClipboardAccess ClipboardAccess { get; }
	};

	/// <summary>
	/// Generic registry that maps type to a value.
	/// </summary>
	public interface IRegistry
	{
		/// <summary>
		/// Sets the value for given type key.
		/// </summary>
		void Set<T>(T value);
		/// <summary>
		/// Gets the value for given type key.
		/// Throws if no value was put for given type.
		/// </summary>
		T Get<T>();
	};

	/// <summary>
	/// A set of utilities to work with test data samples.
	/// Data samples (usually logs) can be large. They are stored
	/// in the cloud storage. The storage is owned by logjoint.
	/// Each plugin has its own storage location that is allocated when plugin is
	/// registered. Samples are immutable - once uploded they never change.
	/// After on-demand downloading, the samples are cached locally.
	/// </summary>
	public interface ISamples
	{
		/// <summary>
		/// Gets full path to local file with given data sample.
		/// Treat the file as read-only in your tests.
		/// Throws is sample with given name does not exist.
		/// </summary>
		Task<string> GetSampleAsLocalFile(string sampleName);
		/// <summary>
		/// Gets a read-only Stream for given sample.
		/// It's caller's responsibility to dispose the Stream
		/// when it's not needed.
		/// Throws is sample with given name does not exist.
		/// </summary>
		Task<Stream> GetSampleAsStream(string sampleName);
		/// <summary>
		/// Gets the absolute Uri for given sample.
		/// Does not throw is sample with given name does not exist.
		/// </summary>
		Uri GetSampleAsUri(string sampleName);
	};

	public interface IUtils
	{
		/// <summary>
		/// Performs API calls that are made when user drags a file
		/// to logjoint from Explorer or Finder.
		/// </summary>
		/// <param name="filePath">Absolute local path to the file</param>
		/// <returns>A Task that is complete when preprocessing of given file has completed.</returns>
		Task EmulateFileDragAndDrop(string filePath);
		/// <summary>
		/// Performs API calls that are made when user drags a URL
		/// to logjoint from a browser.
		/// </summary>
		/// <param name="uri">Absolute URL</param>
		/// <returns>A Task that is complete when preprocessing of given URL has completed,
		/// which usually includes downloading and format detection.</returns>
		Task EmulateUriDragAndDrop(Uri uri);
		/// <summary>
		/// Listens to change notifications checking given condition on each change.
		/// </summary>
		/// <param name="condition">A predicate that detects if expected condition is met</param>
		/// <param name="operationName">Free-form name for logging purpose</param>
		/// <param name="timeout">Time to wait for condition. If condition is not met
		/// during the timeout, the result Task is failed. Timeout is infinite if
		/// value is not specified or null is passed.</param>
		/// <returns>Task that is completed when <paramref name="condition"/> returns true
		/// first time. Task is failed if condition was never met during the timeout</returns>
		Task WaitFor(Func<bool> condition, string operationName = null, TimeSpan? timeout = null);
	};
}