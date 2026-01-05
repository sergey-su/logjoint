using System;

namespace LogJoint.UI.Presenters.NewLogSourceDialog
{
    public interface IPresenter
    {
        void ShowTheDialog(string? selectedPageName = null);
        IPagePresentersRegistry PagesRegistry { get; }
        string FormatDetectorPageName { get; }
    };

    public interface IPagePresenter : IDisposable
    {
        void Apply();
        void Activate();
        void Deactivate();
        object View { get; }
    };

    public interface IPagePresentersRegistry
    {
        IPagePresenter CreatePagePresenter(ILogProviderFactory factory);

        void RegisterPagePresenterFactory(string key, Func<ILogProviderFactory, IPagePresenter> factory);
    };
};