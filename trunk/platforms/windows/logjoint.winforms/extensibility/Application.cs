namespace LogJoint.Extensibility
{
    class Application : IApplication
    {
        public Application(
            IModel model,
            UI.Presenters.IPresentation presentation,
            UI.Windows.IView view
        )
        {
            this.Model = model;
            this.Presentation = presentation;
            this.View = view;
        }

        public UI.Presenters.IPresentation Presentation { get; private set; }
        public IModel Model { get; private set; }
        public UI.Windows.IView View { get; private set; }
    }
}