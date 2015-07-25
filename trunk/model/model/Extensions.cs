using System;
using System.Linq;

namespace LogJoint
{
	public static class ModelExtensions
	{
		public static void DeleteAllLogsAndPreprocessings(this IModel model)
		{
			model.DeleteLogs(model.SourcesManager.Items.Where(s => !s.IsDisposed).ToArray());
			model.DeletePreprocessings(model.LogSourcesPreprocessingManager.Items.Where(s => !s.IsDisposed).ToArray());
		}
	};
}
