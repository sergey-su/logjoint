using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlator;
using System;
using System.IO;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class CorrelatorPostprocessorControlHandler : IViewControlHandler
	{
		readonly IPostprocessorsManager postprocessorsManager;
		readonly ITempFilesManager tempFilesManager;
		readonly string postprocessorId;
		readonly IShellOpen shellOpen;

		public CorrelatorPostprocessorControlHandler(
			IPostprocessorsManager postprocessorsManager,
			ITempFilesManager tempFilesManager,
			IShellOpen shellOpen
		)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.tempFilesManager = tempFilesManager;
			this.postprocessorId = PostprocessorIds.Correlator;
			this.shellOpen = shellOpen;
		}

		ControlData IViewControlHandler.GetCurrentData()
		{
			var state = postprocessorsManager.GetCorrelatorStateSummary();

			if (state.Status == CorrelatorStateSummary.StatusCode.PostprocessingUnavailable)
			{
				return new ControlData()
				{
					Disabled = true,
					Content = "Fix clock skew: N/A"
				};
			}

			var ret = new ControlData();

			ret.Disabled = false;

			switch (state.Status)
			{
				case CorrelatorStateSummary.StatusCode.NeedsProcessing:
					ret.Content = string.Format("Logs clocks may be{0}out of sync.{0}*2 Fix clock skew*", Environment.NewLine);
					ret.Color = ControlData.StatusColor.Warning;
					break;
				case CorrelatorStateSummary.StatusCode.ProcessingInProgress:
					ret.Content = "Fixing clock skew...";
					ret.Progress = state.Progress;
					break;
				case CorrelatorStateSummary.StatusCode.Processed:
				case CorrelatorStateSummary.StatusCode.ProcessingFailed:
					bool wasSuccessful = state.Status == CorrelatorStateSummary.StatusCode.Processed;
					ret.Content = (wasSuccessful ? "Clock skew is fixed." : "Failed to fix clock skew.") + Environment.NewLine;
					if (state.Report != null)
						ret.Content += "*1 View report.* ";
					ret.Content += wasSuccessful ? "*2 Fix again*" : "*2 Try again*";
					if (!wasSuccessful)
						ret.Color = ControlData.StatusColor.Error;
					else
						ret.Color = ControlData.StatusColor.Success;
					break;
			}

			return ret;
		}

		async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			var state = postprocessorsManager.GetCorrelatorStateSummary();
			switch (actionId)
			{
				case "1":
					switch (state.Status)
					{
						case CorrelatorStateSummary.StatusCode.Processed:
						case CorrelatorStateSummary.StatusCode.ProcessingFailed:
							if (state.Report != null)
								ShowTextInTextViewer(state.Report);
							break;
					}
					break;
				case "2":
					switch (state.Status)
					{
						case CorrelatorStateSummary.StatusCode.NeedsProcessing:
						case CorrelatorStateSummary.StatusCode.Processed:
						case CorrelatorStateSummary.StatusCode.ProcessingFailed:
							await this.postprocessorsManager.RunPostprocessors(
								postprocessorsManager.GetPostprocessorOutputsByPostprocessorId(postprocessorId), ClickFlags.None);
							break;
					}
					break;
			}
		}

		void ShowTextInTextViewer(string text)
		{
			var tempFileName = tempFilesManager.GenerateNewName() + ".txt";
			using (var w = new StreamWriter(tempFileName))
				w.Write(text);
			shellOpen.OpenInTextEditor(tempFileName);
		}
	};
}
