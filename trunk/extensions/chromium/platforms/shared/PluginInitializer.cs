using System;
using System.Linq;
using LogJoint.Extensibility;

namespace LogJoint.Chromium
{
	public class PluginInitializer
	{
		public static void Init(IApplication app)
		{
			app.Model.Postprocessing.TimeSeriesTypes.RegisterTimeSeriesTypesAssembly(typeof(Chromium.TimeSeries.PostprocessorsFactory).Assembly);

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.PostprocessorsManager, 
				app.Model.UserDefinedFormatsManager, 
				new Chromium.StateInspector.PostprocessorsFactory(),
				new Chromium.TimeSeries.PostprocessorsFactory(app.Model.Postprocessing.TimeSeriesTypes),
				new Chromium.Correlator.PostprocessorsFactory(app.Model),
				new Chromium.Timeline.PostprocessorsFactory()
			);


			UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter timeSeriesPresenter = null;
			UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm timeSeriesForm = null;

			app.Presentation.PostprocessorsFormFactory.FormCreated += (sender, evt) =>
			{
				if (evt.Id == UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.StateInspector)
				{
					var stateInspectorPresenter = evt.Presenter as UI.Presenters.Postprocessing.StateInspectorVisualizer.IPresenter;
					if (stateInspectorPresenter != null)
					{
						stateInspectorPresenter.OnNodeCreated += (senderPresenter, arg) =>
						{
							if (Chromium.ChromeDebugLog.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject))
								arg.CreateCollapsed = true;
							else if (Chromium.WebrtcInternalsDump.WebRtcStateInspector.ShouldBePresentedCollapsed(arg.NodeObject))
								arg.CreateCollapsed = true;
						};
						stateInspectorPresenter.OnMenu += (senderPresenter, arg) =>
						{
							if (stateInspectorPresenter.SelectedObject != null)
							{
								if (WebrtcInternalsDump.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject)
								 || ChromeDebugLog.WebRtcStateInspector.HasTimeSeries(stateInspectorPresenter.SelectedObject))
								{
									app.Presentation.PostprocessorsFormFactory.GetPostprocessorOutputForm(UI.Presenters.Postprocessing.MainWindowTabPage.ViewControlId.TimeSeries);
									Predicate<UI.Presenters.Postprocessing.TimeSeriesVisualizer.TreeNodeData> predicate = node => 
										node.Type == UI.Presenters.Postprocessing.TimeSeriesVisualizer.ConfigDialogNodeType.ObjectIdGroup
										&& node.Caption.Contains(stateInspectorPresenter.SelectedObject.Id)
										&& stateInspectorPresenter.SelectedObject.Owner.Outputs.Any(x => x.LogSource == node.Owner.LogSource);
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
		}
	}
}
