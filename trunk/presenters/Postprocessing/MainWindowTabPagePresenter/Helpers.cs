using LogJoint.Postprocessing;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public static class Extensions
	{
		public static bool IsLogsCollectionControl(this ViewControlId viewId)
		{
			return viewId == ViewControlId.LogsCollectionControl3 || viewId == ViewControlId.LogsCollectionControl2 || viewId == ViewControlId.LogsCollectionControl1;
		}

		public static string ToDisplayString(this PostprocessorKind kind)
		{
			switch (kind)
			{
				case PostprocessorKind.Correlator: return "Logs correlation";
				case PostprocessorKind.TimeSeries: return "Time Series";
				default: return kind.ToString();
			}
		}
	};
}
