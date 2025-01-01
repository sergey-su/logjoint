﻿using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using System;
using System.IO;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
    class CorrelatorPostprocessorControlHandler : IViewControlHandler
    {
        readonly ICorrelationManager correlationManager;
        readonly ITempFilesManager tempFilesManager;
        readonly IShellOpen shellOpen;
        readonly Func<ControlData> getControlData;

        public CorrelatorPostprocessorControlHandler(
            ICorrelationManager correlationManager,
            ITempFilesManager tempFilesManager,
            IShellOpen shellOpen
        )
        {
            this.correlationManager = correlationManager;
            this.tempFilesManager = tempFilesManager;
            this.shellOpen = shellOpen;
            this.getControlData = Selectors.Create(
                () => correlationManager.StateSummary,
                GetCurrentData
            );
        }

        ControlData IViewControlHandler.GetCurrentData() => getControlData();

        static ControlData GetCurrentData(CorrelationStateSummary state)
        {
            if (state.Status == CorrelationStateSummary.StatusCode.PostprocessingUnavailable)
            {
                return new ControlData(true, "Fix clock skew: N/A");
            }

            string content = null;
            ControlData.StatusColor color = ControlData.StatusColor.Neutral;
            double? progress = null;

            switch (state.Status)
            {
                case CorrelationStateSummary.StatusCode.NeedsProcessing:
                    content = string.Format("Logs clocks may be{0}out of sync.{0}*{1} Fix clock skew*", Environment.NewLine, Constants.RunActionId);
                    color = ControlData.StatusColor.Warning;
                    break;
                case CorrelationStateSummary.StatusCode.ProcessingInProgress:
                    content = "Fixing clock skew...";
                    progress = state.Progress;
                    break;
                case CorrelationStateSummary.StatusCode.Processed:
                case CorrelationStateSummary.StatusCode.ProcessingFailed:
                    bool wasSuccessful = state.Status == CorrelationStateSummary.StatusCode.Processed;
                    content = (wasSuccessful ? "Clock skew is fixed." : "Failed to fix clock skew.") + Environment.NewLine;
                    if (state.Report != null)
                        content += "*1 View report.* ";
                    content += wasSuccessful ? $"*{Constants.RunActionId} Fix again*" : $"*{Constants.RunActionId} Try again*";
                    if (!wasSuccessful)
                        color = ControlData.StatusColor.Error;
                    else
                        color = ControlData.StatusColor.Success;
                    break;
            }

            return new ControlData(false, content, color, progress);
        }

        void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
        {
            var state = correlationManager.StateSummary;
            switch (actionId)
            {
                case "1":
                    switch (state.Status)
                    {
                        case CorrelationStateSummary.StatusCode.Processed:
                        case CorrelationStateSummary.StatusCode.ProcessingFailed:
                            if (state.Report != null)
                                ShowTextInTextViewer(state.Report);
                            break;
                    }
                    break;
                case Constants.RunActionId:
                    switch (state.Status)
                    {
                        case CorrelationStateSummary.StatusCode.NeedsProcessing:
                        case CorrelationStateSummary.StatusCode.Processed:
                        case CorrelationStateSummary.StatusCode.ProcessingFailed:
                            this.correlationManager.Run();
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
