using System;

namespace LogJoint.Chromium.UI.Presenters
{
    public static class Factory
    {
        static public void Create(LogJoint.UI.Presenters.IPresentation presentation)
        {
            var stateInspectorPresenter = presentation.Postprocessing.StateInspector;
            stateInspectorPresenter.OnNodeCreated += (senderPresenter, arg) =>
            {
                if (ChromeDebugLog.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent, arg.NodeObject?.Id, arg.NodeObject?.Parent?.Id))
                    arg.CreateCollapsed = true;
                else if (WebrtcInternalsDump.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent))
                    arg.CreateCollapsed = true;
            };
            stateInspectorPresenter.OnMenu += (senderPresenter, arg) =>
            {
                if (stateInspectorPresenter.SelectedObject != null)
                {
                    if (WebrtcInternalsDump.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject.CreationEvent)
                     || ChromeDebugLog.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject.CreationEvent))
                    {
                        var timeSeriesPresenter = presentation.Postprocessing.TimeSeries;
                        bool predicate(LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.ITreeNodeData node) =>
                            node.Type == LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.ConfigDialogNodeType.ObjectIdGroup
                            && node.Caption.Contains(stateInspectorPresenter.SelectedObject.Id)
                            && stateInspectorPresenter.SelectedObject.BelongsToSource(node.LogSource);
                        if (timeSeriesPresenter.ConfigNodeExists(predicate))
                        {
                            arg.Items.Add(new LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData.Item(
                                "Go to time series",
                                () =>
                                {
                                    timeSeriesPresenter.Show();
                                    timeSeriesPresenter.OpenConfigDialog();
                                    timeSeriesPresenter.SelectConfigNode(predicate);
                                }
                            ));
                        }
                    }
                }
            };
        }
    }
}
