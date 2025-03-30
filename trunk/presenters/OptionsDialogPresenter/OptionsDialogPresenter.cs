using System;

namespace LogJoint.UI.Presenters.Options.Dialog
{
    internal class Presenter : IPresenter, IDialogViewModel
    {
        public Presenter(
            IView view,
            MemAndPerformancePage.IPresenterInternal memAndPerformancePagePresenter,
            Appearance.IPresenterInternal appearancePresenter,
            UpdatesAndFeedback.IPresenterInternal updatesAndFeedbackPresenter,
            Plugins.IPresenterInternal pluginPresenter
        )
        {
            this.view = view;
            this.memAndPerformancePagePresenter = memAndPerformancePagePresenter;
            this.appearancePresenter = appearancePresenter;
            this.updatesAndFeedbackPresenter = updatesAndFeedbackPresenter;
            this.pluginPresenter = pluginPresenter;
        }

        void IPresenter.ShowDialog(PageId? initiallySelectedPage)
        {
            using var dialog = view.CreateDialog(this);
            LoadPages();
            currentDialog = dialog;
            if (initiallySelectedPage != null && (GetVisiblePages() & initiallySelectedPage.Value) == 0)
                initiallySelectedPage = null;
            currentDialog.Show(initiallySelectedPage);
            currentDialog = null;
        }

        void IDialogViewModel.OnOkPressed()
        {
            if (memAndPerformancePagePresenter?.Apply() == false)
                return;
            if (appearancePresenter?.Apply() == false)
                return;
            if (pluginPresenter?.Apply() == false)
                return;
            currentDialog.Hide();
            UnloadPages();
        }

        void IDialogViewModel.OnCancelPressed()
        {
            currentDialog.Hide();
            UnloadPages();
        }

        PageId IDialogViewModel.VisiblePages => GetVisiblePages();

        Appearance.IViewModel IDialogViewModel.AppearancePage => appearancePresenter;

        MemAndPerformancePage.IViewModel IDialogViewModel.MemAndPerformancePage => memAndPerformancePagePresenter;

        UpdatesAndFeedback.IViewModel IDialogViewModel.UpdatesAndFeedbackPage => updatesAndFeedbackPresenter;

        Plugins.IViewModel IDialogViewModel.PluginsPage => pluginPresenter;

        #region Implementation

        void LoadPages()
        {
            memAndPerformancePagePresenter.Load();
            updatesAndFeedbackPresenter.Load();
            appearancePresenter.Load();
            pluginPresenter.Load();
        }

        void UnloadPages()
        {
            pluginPresenter.Unload();
        }

        PageId GetVisiblePages()
        {
            PageId result = PageId.Appearance | PageId.MemAndPerformance;
            if (updatesAndFeedbackPresenter?.IsAvailable == true)
                result |= PageId.UpdatesAndFeedback;
            if (pluginPresenter?.IsAvailable == true)
                result |= PageId.Plugins;
            return result;
        }

        readonly IView view;

        IDialog currentDialog;
        MemAndPerformancePage.IPresenterInternal memAndPerformancePagePresenter;
        Appearance.IPresenterInternal appearancePresenter;
        UpdatesAndFeedback.IPresenterInternal updatesAndFeedbackPresenter;
        Plugins.IPresenterInternal pluginPresenter;

        #endregion
    };
};