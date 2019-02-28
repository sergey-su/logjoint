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
			await postprocessorsManager.RunPostprocessors(GetRelevantLogSourcePostprocessors().ToArray());
		}

		private IEnumerable<LogSourcePostprocessorOutput> GetRelevantLogSourcePostprocessors()
		{
			return postprocessorsManager.GetAutoPostprocessingCapableOutputs();
		}
	};
}
