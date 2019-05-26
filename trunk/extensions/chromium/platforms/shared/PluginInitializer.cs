using System;

namespace LogJoint.Chromium
{
	public class PluginInitializer
	{
		public static void Init(IApplication app)
		{
			app.Model.Postprocessing.TimeSeries.RegisterTimeSeriesTypesAssembly(typeof(TimeSeries.PostprocessorsFactory).Assembly);

			var pluginModel = new PluginModel();

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager, 
				app.Model.UserDefinedFormatsManager, 
				new StateInspector.PostprocessorsFactory(app.Model.Postprocessing, pluginModel),
				new TimeSeries.PostprocessorsFactory(app.Model.Postprocessing, pluginModel),
				new Correlator.PostprocessorsFactory(app.Model),
				new Timeline.PostprocessorsFactory(app.Model.Postprocessing, pluginModel),
				new SequenceDiagram.PostprocessorsFactory(app.Model.Postprocessing)
			);

			app.Model.PluginsManager.Register<IPluginModel>(pluginModel);

			UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter timeSeriesPresenter = null;
			UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm timeSeriesForm = null;

			app.Presentation.PostprocessorsFormFactory.FormCreated += (sender, evt) =>
			{
				if (evt.Id == UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.StateInspector)
				{
					if (evt.Presenter is UI.Presenters.Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorPresenter)
					{
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
									app.Presentation.PostprocessorsFormFactory.GetPostprocessorOutputForm(UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries);
									Predicate<UI.Presenters.Postprocessing.TimeSeriesVisualizer.ITreeNodeData> predicate = node =>
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

			app.Model.PreprocessingManagerExtensionsRegistry.Register(
				new WebrtcInternalsDump.PreprocessingManagerExtension(app.Model.PreprocessingStepsFactory)
			);
			app.Model.PreprocessingManagerExtensionsRegistry.Register(
				new ChromeDriver.PreprocessingManagerExtension(app.Model.PreprocessingStepsFactory, postprocessorsRegistry.ChromeDriver.LogProviderFactory, app.Model.Postprocessing.TextLogParser)
			);
			app.Model.PreprocessingManagerExtensionsRegistry.Register(
				new HttpArchive.PreprocessingManagerExtension(app.Model.PreprocessingStepsFactory, postprocessorsRegistry.HttpArchive.LogProviderFactory)
			);
		}
	}
}
