namespace LogJoint
{
	public class Plugin
	{
		public Plugin(IModel model, UI.Presenters.IPresentation presentation)
		{
			Chromium.Factory.Create(model);
			Chromium.UI.Presenters.Factory.Create(presentation);
		}
	}
}
