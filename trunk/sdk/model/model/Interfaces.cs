using LogJoint.Analytics;
using LogJoint.Analytics.StateInspector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IModel
	{
		ISynchronizationContext ModelThreadSynchronization { get; }
		IChangeNotification ChangeNotification { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IContentCache ContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }
		IBookmarks Bookmarks { get; }
		ILogSourcesManager SourcesManager { get; }
		IModelThreads Threads { get; }
		ITempFilesManager TempFilesManager { get; }
		Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtensionsRegistry { get; }
		Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; }
		Preprocessing.IPreprocessingStepsFactory PreprocessingStepsFactory { get; }
		Progress.IProgressAggregator ProgressAggregator { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		MRU.IRecentlyUsedEntities MRU { get; }
		Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; }
		ILogSourcesController LogSourcesController { get; }
		IShutdown Shutdown { get; }
		WebBrowserDownloader.IDownloader WebBrowserDownloader { get; }
		Postprocessing.IModel Postprocessing { get; }
	};

	namespace Postprocessing
	{

		public interface IModel // todo: move to appropriate folder
		{
			Postprocessing.IPostprocessorsManager PostprocessorsManager { get; }
			Analytics.TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; }
			Analytics.IPrefixMatcher CreatePrefixMatcher();
			Analytics.Correlation.ICorrelator CreateCorrelator();
			// StateInspector.IModel StateInspector { get; }
		};

		namespace StateInspector
		{
			public interface IModel
			{
				Task SavePostprocessorOutput(
					IEnumerableAsync<Event[]> events,
					Task<ILogPartToken> rotatedLogPartToken,
					Func<object, TextLogEventTrigger> triggersConverter,
					LogSourcePostprocessorInput postprocessorInput
				);
			};
		}
	}
}
