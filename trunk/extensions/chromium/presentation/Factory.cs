using System;
using LogJoint;

namespace LogJoint.Chromium.UI.Presenters
{
	public class Factory
	{
		static public void Create(LogJoint.UI.Presenters.IPresentation presentation)
		{
			LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter timeSeriesPresenter = null;
			LogJoint.UI.Presenters.Postprocessing.IPostprocessorOutputForm timeSeriesForm = null;

			presentation.PostprocessorsFormFactory.FormCreated += (sender, evt) =>
			{
				if (evt.Id == LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.StateInspector)
				{
					if (evt.Presenter is LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorPresenter)
					{
						stateInspectorPresenter.OnNodeCreated += (senderPresenter, arg) =>
						{
							if (Chromium.ChromeDebugLog.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent, arg.NodeObject?.Id, arg.NodeObject?.Parent?.Id))
								arg.CreateCollapsed = true;
							else if (Chromium.WebrtcInternalsDump.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent))
								arg.CreateCollapsed = true;
						};
						stateInspectorPresenter.OnMenu += (senderPresenter, arg) =>
						{
							if (stateInspectorPresenter.SelectedObject != null)
							{
								if (Chromium.WebrtcInternalsDump.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject.CreationEvent)
									|| Chromium.ChromeDebugLog.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject.CreationEvent))
								{
									presentation.PostprocessorsFormFactory.GetPostprocessorOutputForm(LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries);
									Predicate<LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.ITreeNodeData> predicate = node =>
										node.Type == LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.ConfigDialogNodeType.ObjectIdGroup
										&& node.Caption.Contains(stateInspectorPresenter.SelectedObject.Id)
										&& stateInspectorPresenter.SelectedObject.BelongsToSource(node.LogSource);
									if (timeSeriesPresenter != null && timeSeriesPresenter.ConfigNodeExists(predicate))
									{
										arg.Items.Add(new LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData.Item()
										{
											Text = "Go to time series",
											Click = () =>
											{
												timeSeriesForm.Show();
												timeSeriesPresenter.OpenConfigDialog();
												timeSeriesPresenter.SelectConfigNode(predicate);
											}
										});
									}
								}
							}
						};
					}
				}
				else if (evt.Id == LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries)
				{
					timeSeriesPresenter = evt.Presenter as LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter;
					timeSeriesForm = evt.Form;
				}
			};
		}
	}
}
