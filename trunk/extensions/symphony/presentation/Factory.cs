using System;
using LogJoint;

namespace LogJoint.Symphony.UI.Presenters
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
							if (Rtc.MeetingsStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent))
								arg.CreateCollapsed = true;
							else if (Rtc.MediaStateInspector.ShouldBePresentedCollapsed(arg.NodeObject?.CreationEvent))
								arg.CreateCollapsed = true;
						};
						stateInspectorPresenter.OnMenu += (senderPresenter, arg) =>
						{
							if (stateInspectorPresenter.SelectedObject != null)
							{
								if (Rtc.MediaStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject.CreationEvent))
								{
									presentation.PostprocessorsFormFactory.GetPostprocessorOutputForm(LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries);
									bool predicate(LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.ITreeNodeData node) =>
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
#if MONOMAC
								IVisualizerNode GetParent(IVisualizerNode n) => n.Parent == null ? n : GetParent(n.Parent);
								var (id, referenceTime, env) = Rtc.MeetingsStateInspector.GetMeetingRelatedId(
									stateInspectorPresenter.SelectedObject.CreationEvent, stateInspectorPresenter.SelectedObject.ChangeHistory,
									GetParent(stateInspectorPresenter.SelectedObject).CreationEvent, GetParent(stateInspectorPresenter.SelectedObject).ChangeHistory
								);
								if (id != null)
								{
									arg.Items.Add(new LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData.Item()
									{
										Text = "Download backend logs",
										Click = () =>
										{
											var input = presentation.PromptDialog.ExecuteDialog(
												"Download RTC backend logs",
												"Specify query parameters",
												$"ID={id}{Environment.NewLine}Environment={env ?? "(undetected)"}{Environment.NewLine}Reference time={referenceTime.ToString("o")}");
											if (input != null)
											{
												var ids = new [] { id };
												foreach (var line in input.Split('\r', '\n'))
												{
													var m = Regex.Match(line, @"^(?<k>[^\=]+)\=(?<v>.+)$", RegexOptions.ExplicitCapture);
													if (!m.Success)
														continue;
													var k = m.Groups["k"].Value;
													var v = m.Groups["v"].Value;
													if (k == "ID")
														ids = v.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
													else if (k == "Environment")
														env = v;
													else if (k == "Reference time")
														if (DateTime.TryParseExact(v, "o", null, System.Globalization.DateTimeStyles.None, out var tmpRefTime))
															referenceTime = tmpRefTime;
												}
												app.Model.Preprocessing.Manager.Preprocess(
													new[] { backendLogsPreprocessingStepsFactory.CreateDownloadBackendLogsStep(ids, referenceTime, env) },
													"Downloading backend logs",
													Preprocessing.PreprocessingOptions.HighlightNewPreprocessing
												);
											}
										}
									});
								}
#endif
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
