using System;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public static class ModelExtensions
	{
		public static async Task DeleteAllLogsAndPreprocessings(this IModel model)
		{
			await model.DeleteLogs(model.SourcesManager.Items.Where(s => !s.IsDisposed).ToArray());
			await model.DeletePreprocessings(model.LogSourcesPreprocessingManager.Items.Where(s => !s.IsDisposed).ToArray());
		}
	};
}
