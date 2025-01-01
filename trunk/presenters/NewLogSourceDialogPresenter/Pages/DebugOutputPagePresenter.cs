using System;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.DebugOutput
{
    public interface IView
    {
        object PageView { get; }
    };

    public class Presenter : IPagePresenter
    {
        readonly IView view;
        readonly ILogProviderFactory factory;
        readonly ILogSourcesManager model;

        public Presenter(IView view, ILogProviderFactory factory, ILogSourcesManager model)
        {
            this.view = view;
            this.factory = factory;
            this.model = model;
        }

        async void IPagePresenter.Apply()
        {
            await model.Create(factory, new ConnectionParams());
        }

        void IPagePresenter.Activate()
        {
        }

        void IPagePresenter.Deactivate()
        {
        }

        object IPagePresenter.View
        {
            get { return view.PageView; }
        }

        void IDisposable.Dispose()
        {
        }
    };
};