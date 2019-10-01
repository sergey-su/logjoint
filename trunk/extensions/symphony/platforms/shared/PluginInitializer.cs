using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Symphony
{
	public class PluginImpl
	{
		public PluginImpl(IApplication app)
		{
			app.Model.Postprocessing.TimeSeries.RegisterTimeSeriesTypesAssembly(typeof(TimeSeries.PostprocessorsFactory).Assembly);

			StateInspector.IPostprocessorsFactory statePostprocessors = new StateInspector.PostprocessorsFactory(
				app.Model.TempFilesManager,
				app.Model.Postprocessing
			);

			TimeSeries.IPostprocessorsFactory timeSeriesPostprocessors = new TimeSeries.PostprocessorsFactory(
				app.Model.Postprocessing
			);

			Timeline.IPostprocessorsFactory timelinePostprocessors = new Timeline.PostprocessorsFactory(
				app.Model.TempFilesManager,
				app.Model.Postprocessing
			);

			SequenceDiagram.IPostprocessorsFactory sequenceDiagramPostprocessors = new SequenceDiagram.PostprocessorsFactory(
				app.Model.Postprocessing
			);

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager,
				app.Model.UserDefinedFormatsManager,
				statePostprocessors,
				timeSeriesPostprocessors,
				new Correlator.PostprocessorsFactory(app.Model),
				timelinePostprocessors,
				sequenceDiagramPostprocessors
			);

			var chromiumPlugin = app.Model.PluginsManager.Get<Chromium.IPluginModel>();
			if (chromiumPlugin != null)
			{
				chromiumPlugin.RegisterSource(statePostprocessors.CreateChromeDebugSourceFactory());
				chromiumPlugin.RegisterSource(timeSeriesPostprocessors.CreateChromeDebugSourceFactory());
				chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDebugLogEventsSourceFactory());
				chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDriverEventsSourceFactory());
				chromiumPlugin.RegisterSource(sequenceDiagramPostprocessors.CreateChromeDebugLogEventsSourceFactory());
			}

			app.Model.Preprocessing.ExtensionsRegistry.AddLogDownloaderRule(
				new Uri("https://perzoinc.atlassian.net/secure/attachment/"),
				Preprocessing.LogDownloaderRule.CreateBrowserDownloaderRule(new[] { "https://id.atlassian.com/login" })
			);

#if MONOMAC
			SpringServiceLog.IPreprocessingStepsFactory backendLogsPreprocessingStepsFactory = new SpringServiceLog.PreprocessingStepsFactory(
				app.Model.Preprocessing.StepsFactory,
				app.Model.WebViewTools,
				app.Model.ContentCache
			);
			app.Model.Preprocessing.ExtensionsRegistry.Register(new SpringServiceLog.PreprocessingManagerExtension(
				backendLogsPreprocessingStepsFactory));
#endif

			UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter timeSeriesPresenter = null;
			UI.Presenters.Postprocessing.IPostprocessorOutputForm timeSeriesForm = null;

			app.Presentation.PostprocessorsFormFactory.FormCreated += (sender, evt) =>
			{
				if (evt.Id == UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.StateInspector)
				{
					if (evt.Presenter is UI.Presenters.Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorPresenter)
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
									app.Presentation.PostprocessorsFormFactory.GetPostprocessorOutputForm(UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries);
									bool predicate(UI.Presenters.Postprocessing.TimeSeriesVisualizer.ITreeNodeData node) =>
										   node.Type == UI.Presenters.Postprocessing.TimeSeriesVisualizer.ConfigDialogNodeType.ObjectIdGroup
										&& node.Caption.Contains(stateInspectorPresenter.SelectedObject.Id)
										&& stateInspectorPresenter.SelectedObject.BelongsToSource(node.LogSource);
									if (timeSeriesPresenter != null && timeSeriesPresenter.ConfigNodeExists(predicate))
									{
										arg.Items.Add(new UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData.Item()
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
									arg.Items.Add(new UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData.Item()
									{
										Text = "Download backend logs",
										Click = () =>
										{
											var input = app.Presentation.PromptDialog.ExecuteDialog(
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
				else if (evt.Id == UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries)
				{
					timeSeriesPresenter = evt.Presenter as UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter;
					timeSeriesForm = evt.Form;
				}
			};
		}
	}
}
