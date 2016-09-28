using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.MRU;
using System.Threading.Tasks;
using LogJoint.Preprocessing;

namespace LogJoint
{
	public class LogSourcesController : ILogSourcesController
	{
		readonly ILogSourcesManager logSources;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly IRecentlyUsedEntities mruLogsList;

		public LogSourcesController(
			ILogSourcesManager logSources,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings,
			IRecentlyUsedEntities mruLogsList,
			IShutdown shutdown
		)
		{
			this.logSources = logSources;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.mruLogsList = mruLogsList;

			this.logSources.OnLogSourceAnnotationChanged += (s, e) =>
			{
				var source = (ILogSource)s;
				mruLogsList.UpdateRecentLogEntry(source.Provider, source.Annotation);
			};
			this.logSourcesPreprocessings.ProviderYielded += (sender, yieldedProvider) =>
			{
				CreateLogSourceInternal(yieldedProvider.Factory, yieldedProvider.ConnectionParams, yieldedProvider.IsHiddenLog);
			};

			shutdown.Cleanup += (sender, e) => 
			{
				shutdown.AddCleanupTask(Dispose());
			};
		}

		ILogSource ILogSourcesController.CreateLogSource (ILogProviderFactory factory, IConnectionParams connectionParams)
		{
			return CreateLogSourceInternal(factory, connectionParams, makeHidden: false);
		}

		async Task ILogSourcesController.DeleteAllLogsAndPreprocessings()
		{
			var tasks = new []
			{
				logSources.DeleteAllLogs(),
				logSourcesPreprocessings.DeleteAllPreprocessings()
			};
			await Task.WhenAll(tasks);
		}

		ILogSource CreateLogSourceInternal(ILogProviderFactory factory, IConnectionParams cp, bool makeHidden)
		{
			ILogSource src = logSources.FindLiveLogSourceOrCreateNew(factory, cp);
			src.Visible = !makeHidden;
			mruLogsList.RegisterRecentLogEntry(src.Provider, src.Annotation);
			return src;
		}

		async Task Dispose()
		{
			await logSources.DeleteAllLogs();
			await logSourcesPreprocessings.DeleteAllPreprocessings();
		}
	};
}
