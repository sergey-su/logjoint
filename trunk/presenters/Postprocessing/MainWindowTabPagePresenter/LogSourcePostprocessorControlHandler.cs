using LogJoint.Postprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	class LogSourcePostprocessorControlHandler : IViewControlHandler
	{
		readonly IPostprocessorsManager postprocessorsManager;
		readonly PostprocessorKind postprocessorKind;
		readonly Func<IPostprocessorOutputForm> lazyOutputForm;
		readonly LogJoint.UI.Presenters.IShellOpen shellOpen;
		readonly ITempFilesManager tempFiles;

		public LogSourcePostprocessorControlHandler(
			IPostprocessorsManager postprocessorsManager,
			PostprocessorKind postprocessorKind,
			Func<IPostprocessorOutputForm> lazyOutputForm,
			LogJoint.UI.Presenters.IShellOpen shellOpen,
			ITempFilesManager tempFiles
		)
		{
			this.postprocessorsManager = postprocessorsManager;
			this.postprocessorKind = postprocessorKind;
			this.lazyOutputForm = lazyOutputForm;
			this.shellOpen = shellOpen;
			this.tempFiles = tempFiles;
		}

		ControlData IViewControlHandler.GetCurrentData()
		{
			var outputs = postprocessorsManager.GetPostprocessorOutputsByPostprocessorId(postprocessorKind);

			if (outputs.Length == 0)
			{
				return new ControlData()
				{
					Disabled = true,
					Content = postprocessorKind.ToDisplayString() + ": N/A"
				};
			}

			int nrOfRunning = 0;
			int nrOfLoading = 0;
			int nrOfProcessed = 0;
			int nrOfUnprocessed = 0;
			int nrOfOutdated = 0;
			int nrOfProcessedWithWarnings = 0;
			int nrOfProcessedWithErrors = 0;
			double? progress = null;
			foreach (var output in outputs)
			{
				switch (output.OutputStatus)
				{
					case LogSourcePostprocessorOutput.Status.Finished:
					case LogSourcePostprocessorOutput.Status.Failed:
						if (output.LastRunSummary != null)
						{
							if (output.LastRunSummary.HasWarnings)
								++nrOfProcessedWithWarnings;
							if (output.LastRunSummary.HasErrors)
								++nrOfProcessedWithErrors;
						}
						++nrOfProcessed;
						break;
					case LogSourcePostprocessorOutput.Status.Outdated:
						++nrOfOutdated;
						++nrOfProcessed;
						break;
					case LogSourcePostprocessorOutput.Status.InProgress:
						++nrOfRunning;
						progress = output.Progress;
						break;
					case LogSourcePostprocessorOutput.Status.Loading:
						++nrOfLoading;
						progress = output.Progress;
						break;
					case LogSourcePostprocessorOutput.Status.NeverRun:
						++nrOfUnprocessed;
						break;
				}
			}

			var ret = new ControlData();
			var isClickableCaption = false;
			string action = null;
			string statusText = null;

			ret.Disabled = false;
			
			Action appendReportLinkIfRequired = () =>
			{
				if (nrOfProcessedWithErrors > 0)
					statusText += " *report with errors*";
				else if (nrOfProcessedWithWarnings > 0)
					statusText += " *report with warnings*";
			};

			if (nrOfLoading > 0 && nrOfRunning == 0)
			{
				statusText = string.Format("loading... ({0} of {1} logs completed)", nrOfLoading, nrOfLoading + nrOfProcessed);
				ret.Progress = progress;
			}
			else if (nrOfRunning > 0)
			{
				statusText = string.Format("running... ({0} of {1} logs completed)", nrOfProcessed, nrOfRunning + nrOfProcessed + nrOfLoading);
				ret.Progress = progress;
			}
			else if (nrOfUnprocessed > 0 || nrOfOutdated > 0)
			{
				statusText = string.Format("{0} of {1} logs processed", nrOfProcessed, nrOfProcessed + nrOfUnprocessed);
				appendReportLinkIfRequired();
				if (nrOfOutdated > 0)
					statusText += string.Format(", {0} outdated", nrOfOutdated);
				if (lazyOutputForm != null && nrOfProcessed > 0)
					isClickableCaption = true;
				action = "run postprocessor";
				ret.Color = ControlData.StatusColor.Warning;
			}
			else
			{
				statusText = string.Format("all logs processed");
				if (lazyOutputForm != null && nrOfProcessed > 0)
					isClickableCaption = true;
				appendReportLinkIfRequired();
				action = "re-process";
				if (nrOfProcessedWithErrors > 0)
					ret.Color = ControlData.StatusColor.Error;
				else
					ret.Color = ControlData.StatusColor.Success;
			}

			var contentBuilder = new StringBuilder();
			if (isClickableCaption)
				contentBuilder.AppendFormat("*show {0}:*", outputs[0].PostprocessorMetadata.Kind.ToDisplayString());
			else
				contentBuilder.AppendFormat("{0}:", outputs[0].PostprocessorMetadata.Kind.ToDisplayString());
			if (statusText != null)
				contentBuilder.AppendFormat("  {0}", statusText);
			if (action != null)
				contentBuilder.AppendFormat("  *action {0}*", action);
			ret.Content += contentBuilder.ToString();

			return ret;
		}

		async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
		{
			switch (actionId)
			{
				case "show":
					if (lazyOutputForm != null)
					{
						lazyOutputForm().Show();
					}
					break;
				case "action":
					if (!await this.postprocessorsManager.RunPostprocessors(postprocessorsManager.GetPostprocessorOutputsByPostprocessorId(postprocessorKind)))
					{
						return;
					}
					if (lazyOutputForm != null)
					{
						var outputs = postprocessorsManager.GetPostprocessorOutputsByPostprocessorId(postprocessorKind);
						if (outputs.Any(x => x.OutputStatus == LogSourcePostprocessorOutput.Status.Finished))
						{
							lazyOutputForm().Show();
						}
					}
					break;
				case "report":
					{
						var outputs = postprocessorsManager.GetPostprocessorOutputsByPostprocessorId(postprocessorKind);
						var summaries = 
							outputs
								.Select(output => output.LastRunSummary)
								.Where(summary => summary != null)
								.OrderBy(summary => summary.HasErrors ? 0 : summary.HasWarnings ? 1 : 2);
						var text = new StringBuilder();
						foreach (var summary in summaries)
						{
							text.Append(summary.Report ?? "");
							text.AppendLine();
							text.AppendLine();
						}
						if (text.Length > 0)
						{
							var fname = Path.ChangeExtension(tempFiles.GenerateNewName(), ".txt");
							File.WriteAllText(fname, text.ToString());
							shellOpen.OpenInTextEditor(fname);
						}
					}
					break;
			}
		}
	};
}
