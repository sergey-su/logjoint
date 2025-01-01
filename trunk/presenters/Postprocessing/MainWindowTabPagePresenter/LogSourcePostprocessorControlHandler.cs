using LogJoint.Postprocessing;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
    class LogSourcePostprocessorControlHandler : IViewControlHandler
    {
        readonly IManagerInternal postprocessorsManager;
        readonly PostprocessorKind postprocessorKind;
        readonly SummaryDialog.IPresenter summaryDialogPresenter;
        readonly Func<IPostprocessorVisualizerPresenter> visualizerPresenter;
        readonly IShellOpen shellOpen;
        readonly ITempFilesManager tempFiles;
        readonly Func<ImmutableList<LogSourcePostprocessorState>> getOutputs;
        readonly Func<ControlData> getControlData;


        public LogSourcePostprocessorControlHandler(
            IManagerInternal postprocessorsManager,
            PostprocessorKind postprocessorKind,
            Func<IPostprocessorVisualizerPresenter> visualizerPresenter,
            IShellOpen shellOpen,
            ITempFilesManager tempFiles,
            SummaryDialog.IPresenter summaryDialogPresenter
        )
        {
            this.postprocessorsManager = postprocessorsManager;
            this.postprocessorKind = postprocessorKind;
            this.visualizerPresenter = visualizerPresenter;
            this.shellOpen = shellOpen;
            this.tempFiles = tempFiles;
            this.summaryDialogPresenter = summaryDialogPresenter;
            this.getOutputs = Selectors.Create(
                () => postprocessorsManager.LogSourcePostprocessors,
                outputs => ImmutableList.CreateRange(
                    outputs.Where(output => output.Postprocessor.Kind == postprocessorKind)
                )
            );
            this.getControlData = Selectors.Create(
                getOutputs,
                outputs => GetCurrentData(outputs, postprocessorKind)
            );
        }

        ControlData IViewControlHandler.GetCurrentData() => getControlData();

        static ControlData GetCurrentData(ImmutableList<LogSourcePostprocessorState> outputs, PostprocessorKind postprocessorKind)
        {
            if (outputs.Count == 0)
            {
                return new ControlData(true, postprocessorKind.ToDisplayString() + ": N/A");
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
                    case LogSourcePostprocessorState.Status.Finished:
                    case LogSourcePostprocessorState.Status.Failed:
                        if (output.LastRunSummary != null)
                        {
                            if (output.LastRunSummary.HasWarnings)
                                ++nrOfProcessedWithWarnings;
                            if (output.LastRunSummary.HasErrors)
                                ++nrOfProcessedWithErrors;
                        }
                        ++nrOfProcessed;
                        break;
                    case LogSourcePostprocessorState.Status.Outdated:
                        ++nrOfOutdated;
                        ++nrOfProcessed;
                        break;
                    case LogSourcePostprocessorState.Status.InProgress:
                        ++nrOfRunning;
                        progress = output.Progress;
                        break;
                    case LogSourcePostprocessorState.Status.Loading:
                        ++nrOfLoading;
                        progress = output.Progress;
                        break;
                    case LogSourcePostprocessorState.Status.NeverRun:
                        ++nrOfUnprocessed;
                        break;
                }
            }

            var isClickableCaption = false;
            string action = null;
            string statusText = null;

            string controlContent = "";
            ControlData.StatusColor controlColor = ControlData.StatusColor.Neutral;
            double? controlProgress = null;

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
                controlProgress = progress;
            }
            else if (nrOfRunning > 0)
            {
                statusText = string.Format("running... ({0} of {1} logs completed)", nrOfProcessed, nrOfRunning + nrOfProcessed + nrOfLoading);
                controlProgress = progress;
            }
            else if (nrOfUnprocessed > 0 || nrOfOutdated > 0)
            {
                statusText = string.Format("{0} of {1} logs processed", nrOfProcessed, nrOfProcessed + nrOfUnprocessed);
                appendReportLinkIfRequired();
                if (nrOfOutdated > 0)
                    statusText += string.Format(", {0} outdated", nrOfOutdated);
                if (nrOfProcessed > 0)
                    isClickableCaption = true;
                action = "run postprocessor";
                controlColor = ControlData.StatusColor.Warning;
            }
            else
            {
                statusText = string.Format("all logs processed");
                if (nrOfProcessed > 0)
                    isClickableCaption = true;
                appendReportLinkIfRequired();
                action = "re-process";
                if (nrOfProcessedWithErrors > 0)
                    controlColor = ControlData.StatusColor.Error;
                else
                    controlColor = ControlData.StatusColor.Success;
            }

            var contentBuilder = new StringBuilder();
            if (isClickableCaption)
                contentBuilder.AppendFormat("*{1} {0}:*", outputs[0].Postprocessor.Kind.ToDisplayString(), Constants.ShowVisualizerActionId);
            else
                contentBuilder.AppendFormat("{0}:", outputs[0].Postprocessor.Kind.ToDisplayString());
            if (statusText != null)
                contentBuilder.AppendFormat("  {0}", statusText);
            if (action != null)
                contentBuilder.AppendFormat("  *{1} {0}*", action, Constants.RunActionId);
            controlContent += contentBuilder.ToString();

            return new ControlData(false, controlContent, controlColor, controlProgress);
        }

        async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
        {
            switch (actionId)
            {
                case Constants.ShowVisualizerActionId:
                    visualizerPresenter().Show();
                    break;
                case Constants.RunActionId:
                    {
                        await this.postprocessorsManager.RunPostprocessors(getOutputs());
                        var outputs = getOutputs();
                        if (outputs.Any(x => x.OutputStatus == LogSourcePostprocessorState.Status.Finished))
                        {
                            visualizerPresenter().Show();
                        }
                    }
                    break;
                case "report":
                    {
                        if (summaryDialogPresenter.IsEnabled)
                        {
                            var outputs = getOutputs();
                            var summaries =
                                outputs
                                    .Select(output => (logSource: output.LogSource, summary: output.LastRunSummary as IStructuredPostprocessorRunSummary))
                                    .Where(item => item.summary != null)
                                    .OrderBy(item => item.summary.HasErrors ? 0 : item.summary.HasWarnings ? 1 : 2)
                                    .ToArray();
                            if (summaries.Length > 0)
                            {
                                summaryDialogPresenter.ShowDialog(summaries);
                            }
                        }
                        else
                        {
                            var outputs = getOutputs();
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
                                var fname = Path.Combine(Path.GetDirectoryName(tempFiles.GenerateNewName()),
                                    $"{postprocessorKind}-errors-{DateTime.Now.Ticks}.txt");
                                File.WriteAllText(fname, text.ToString());
                                shellOpen.OpenInTextEditor(fname);
                            }
                        }
                    }
                    break;
            }
        }
    };
}
