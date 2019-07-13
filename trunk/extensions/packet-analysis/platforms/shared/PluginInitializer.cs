namespace LogJoint.PacketAnalysis
{
	public class PluginInitializer
	{
		public static void Init(
			IApplication app,
			UI.Presenters.Factory.IViewsFactory viewsFactory
		)
		{
			var model = Factory.Create(app.Model);
			var presentation = UI.Presenters.Factory.Create(model, app.Presentation, app.Model, viewsFactory);
		}
	}
}
