using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class AllPostprocessorsControlHandler : IViewControlHandler
	{
		readonly IManager postprocessorsManager;
		readonly Func<LogSourcePostprocessorOutput[]> getOutputs;
		readonly Func<ControlData> getControlData;

		public AllPostprocessorsControlHandler(IManager postprocessorsManager)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.getOutputs = Selectors.Create(
				() => postprocessorsManager.LogSourcePostprocessorsOutputs,
				_ => postprocessorsManager.GetAutoPostprocessingCapableOutputs().ToArray()
			);
			this.getControlData = Selectors.Create(
				getOutputs,
				outputs => {
					var relevantPostprocessorExists = outputs.Any();

					return new ControlData(
						!relevantPostprocessorExists,
						"*action Run all postprocessors*",
						ControlData.StatusColor.Neutral
					);
				}
			);
		}

		ControlData IViewControlHandler.GetCurrentData() => getControlData();

		async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			await postprocessorsManager.RunPostprocessors(getOutputs());
		}
	};
}
