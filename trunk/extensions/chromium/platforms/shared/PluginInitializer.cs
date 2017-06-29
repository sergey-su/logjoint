using LogJoint.Extensibility;

namespace LogJoint.Chromium
{
	public class PluginInitializer
	{

		public static void Init(IApplication app)
		{
			app.Model.Postprocessing.TimeSeriesTypes.DefaultTimeSeriesTypesAssembly = typeof(Chromium.TimeSeries.PostprocessorsFactory).Assembly;

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitialilizer(
				app.Model.Postprocessing.PostprocessorsManager, 
				app.Model.UserDefinedFormatsManager, 
				new Chromium.StateInspector.PostprocessorsFactory(),
				new Chromium.TimeSeries.PostprocessorsFactory(app.Model.Postprocessing.TimeSeriesTypes)
			);

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
						};
					}
				}
			};
		}
	}
}
