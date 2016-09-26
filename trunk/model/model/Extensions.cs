using System;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public static class ModelExtensions
	{
		public static async Task DeleteAllLogsAndPreprocessings(
			ILogSourcesManager sourcesManager, 
			Preprocessing.ILogSourcesPreprocessingManager preprocessingManager
		)
		{
			var tasks = new []
			{
				DeleteAllLogs(sourcesManager),
				DeleteAllPreprocessings(preprocessingManager)
			};
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteLogs(this ILogSourcesManager lsm, ILogSource[] sources)
		{
			var tasks = sources.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteAllLogs(this ILogSourcesManager lsm)
		{
			await DeleteLogs(lsm, lsm.Items.ToArray());
		}

		public static async Task DeletePreprocessings(this Preprocessing.ILogSourcesPreprocessingManager lspm, Preprocessing.ILogSourcePreprocessing[] preprs)
		{
			var tasks = preprs.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteAllPreprocessings(this Preprocessing.ILogSourcesPreprocessingManager lspm)
		{
			await DeletePreprocessings(lspm, lspm.Items.ToArray());
		}
	};
}
