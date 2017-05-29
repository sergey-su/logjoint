using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class AllPostprocessorsControlHandler : IViewControlHandler
	{
		readonly IPostprocessorsManager postprocessorsManager;

		public AllPostprocessorsControlHandler(IPostprocessorsManager postprocessorsManager)
		{
			this.postprocessorsManager = postprocessorsManager;
		}

		ControlData IViewControlHandler.GetCurrentData()
		{
			var ret = new ControlData();

			var relevantPostprocessorExists = GetRelevantLogSourcePostprocessors().Any();

			ret.Disabled = !relevantPostprocessorExists;
			ret.Content = "*action Run all postprocessors*";

			return ret;
		}

		async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			await postprocessorsManager.RunPostprocessors(GetRelevantLogSourcePostprocessors().ToArray(), ClickFlags.None);
		}

		private IEnumerable<LogSourcePostprocessorOutput> GetRelevantLogSourcePostprocessors()
		{
			return
				postprocessorsManager
				.LogSourcePostprocessorsOutputs
				.Where(output => IsRelevantPostprocessor(output.PostprocessorMetadata.TypeID) && IsStatusOkToEnableAllPostprocessors(output.OutputStatus));
		}

		static bool IsRelevantPostprocessor(string id)
		{
			return
				id == PostprocessorIds.StateInspector
			 || id == PostprocessorIds.Timeline
			 || id == PostprocessorIds.SequenceDiagram
			 || id == PostprocessorIds.TimeSeries
			 || id == PostprocessorIds.Correlator;
		}

		static bool IsStatusOkToEnableAllPostprocessors(LogSourcePostprocessorOutput.Status value)
		{
			return value != LogSourcePostprocessorOutput.Status.InProgress;
		}
	};
}
