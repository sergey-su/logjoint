namespace LogJoint
{
    public class Plugin
    {
        public Plugin(IModel model, UI.Presenters.IPresentation presentation)
        {
            Chromium.Factory.Create(model);
            Chromium.UI.Presenters.Factory.Create(presentation);
        }
        public Plugin(IModel model)
        {
            Chromium.Factory.Create(model);
        }
    }
}
