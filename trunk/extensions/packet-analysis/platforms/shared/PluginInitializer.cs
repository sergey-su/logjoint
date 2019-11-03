namespace LogJoint.PacketAnalysis
{
	public class PluginInitializer
	{
		public static void Init(
			IModel appModel,
			LogJoint.UI.Presenters.IPresentation appPresentation,
			UI.Presenters.Factory.IViewsFactory viewsFactory
		)
		{
			var model = Factory.Create(appModel);
			var presentation = UI.Presenters.Factory.Create(model, appPresentation, appModel, viewsFactory);
		}
	}
}
