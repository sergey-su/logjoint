
namespace LogJoint.Wasm.Extensibility
{
    public interface IApplication
    {
        IModel Model { get; }
        LogJoint.UI.Presenters.IPresentation Presentation { get; }
        object View { get; }
    };

    class Application : IApplication
    {
        public Application(
            IModel model,
            LogJoint.UI.Presenters.IPresentation presentation
        )
        {
            this.Model = model;
            this.Presentation = presentation;
        }

        public LogJoint.UI.Presenters.IPresentation Presentation { get; private set; }
        public IModel Model { get; private set; }
        public object View { get; private set; }
    }
}
