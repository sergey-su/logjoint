using System;
using System.Linq;

namespace LogJoint.Symphony
{
	public class PluginImpl
	{
		public Action Start;

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

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager,
				app.Model.UserDefinedFormatsManager,
				statePostprocessors,
				timeSeriesPostprocessors,
				new Correlator.PostprocessorsFactory(app.Model),
				timelinePostprocessors,
				new SequenceDiagram.PostprocessorsFactory(app.Model.Postprocessing)
			);

			Start = () =>
			{
				var chromiumPlugin = app.Model.PluginsManager.Get<Chromium.IPluginModel>();
				if (chromiumPlugin != null)
				{
					chromiumPlugin.RegisterSource(statePostprocessors.CreateChromeDebugSourceFactory());
					chromiumPlugin.RegisterSource(timeSeriesPostprocessors.CreateChromeDebugSourceFactory());
					chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDebugLogEventsSourceFactory());
					chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDriverEventsSourceFactory());
				}
			};

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
