using System;
using System.Collections.Generic;
using static LogJoint.Settings.Appearance;

namespace LogJoint.UI.Presenters.Options
{
    namespace Dialog
    {
        public interface IPresenter
        {
            void ShowDialog(PageId? initiallySelectedPage = null);
        };

        public interface IView
        {
            IDialog CreateDialog(IDialogViewModel viewModel);
        };


        public interface IDialog : IDisposable // todo: remove, make reactive
        {
            void Show(PageId? initiallySelectedPage);
            void Hide();
        };

        public interface IDialogViewModel
        {
            void OnOkPressed();
            void OnCancelPressed();
            PageId VisiblePages { get; }
            Appearance.IViewModel AppearancePage { get; }
            MemAndPerformancePage.IViewModel MemAndPerformancePage { get; }
            UpdatesAndFeedback.IViewModel UpdatesAndFeedbackPage { get; }
            Plugins.IViewModel PluginsPage { get; }
        };

        public interface IPagePresenter
        {
            void Apply();
        };

        [Flags]
        public enum PageId
        {
            None = 0,
            MemAndPerformance = 1,
            Appearance = 2,
            Plugins = 4,
            UpdatesAndFeedback = 8,
        };
    }

    namespace MemAndPerformancePage
    {
        public interface IPresenter
        {
            bool Apply();
        };

        public interface IView // todo: remove, make reactive
        {
            bool GetControlEnabled(ViewControl control);
            void SetControlEnabled(ViewControl control, bool value);
            bool GetControlChecked(ViewControl control);
            void SetControlChecked(ViewControl control, bool value);
            void FocusControl(ViewControl control);
            bool ShowConfirmationDialog(string message);
            void SetControlText(ViewControl ctrlId, string value);
        };

        public enum ViewControl
        {
            ClearRecentEntriesListLinkLabel,
            ClearSearchHistoryLinkLabel,
            LogSpecificStorageEnabledCheckBox,
            ClearLogSpecificStorageLinkLabel,
            DisableMultithreadedParsingCheckBox,
            MemoryConsumptionLabel,
            CollectUnusedMemoryLinkLabel,
            EnableAutoPostprocessingCheckBox,
        };

        public interface IViewModel
        {
            void SetView(IView view);

            IChangeNotification ChangeNotification { get; }

            void OnLinkClicked(ViewControl control);
            void OnCheckboxChecked(ViewControl control);

            LabeledStepperPresenter.IViewModel RecentLogsListSizeEditor { get; }
            LabeledStepperPresenter.IViewModel SearchHistoryDepthEditor { get; }
            LabeledStepperPresenter.IViewModel MaxNumberOfSearchResultsEditor { get; }
            LabeledStepperPresenter.IViewModel LogSizeThresholdEditor { get; }
            LabeledStepperPresenter.IViewModel LogWindowSizeEditor { get; }
        };

        internal interface IPresenterInternal : IPresenter, IViewModel { };
    };

    namespace Appearance
    {
        public interface IPresenter : IDisposable
        {
            bool Apply();
        };

        public interface IView
        {
            string[] AvailablePreferredFamilies { get; }
            KeyValuePair<LogFontSize, int>[] FontSizes { get; }
            void SetSelectorControl(ViewControl selector, string[] options, int selectedOption);
            int GetSelectedValue(ViewControl selector);
        };

        public enum ViewControl
        {
            ColoringSelector,
            FontFamilySelector,
            PaletteSelector
        };

        public interface IViewModel
        {
            void SetView(IView view);

            LogViewer.IViewModel LogView { get; }

            LabeledStepperPresenter.IViewModel FontSizeControl { get; }

            void OnSelectedValueChanged(ViewControl ctrl);
        };

        internal interface IPresenterInternal: IPresenter, IViewModel
        {
        };
    };

    namespace UpdatesAndFeedback
    {
        public interface IPresenter
        {
            bool IsAvailable { get; }
            bool Apply();
        };

        public interface IView // todo: remove, make reactive
        {
            void SetLastUpdateCheckInfo(string brief, string details);
            void SetCheckNowButtonAvailability(bool canCheckNow);
        };

        public interface IViewModel
        {
            void SetView(IView view);
            void OnCheckUpdateNowClicked();
        };

        internal interface IPresenterInternal : IPresenter, IViewModel {};
    };

    namespace Plugins
    {
        public interface IPresenter : IDisposable, IPageAvailability
        {
            bool Apply();
        };

        public interface IViewModel
        {
            IChangeNotification ChangeNotification { get; }
            IReadOnlyList<IPluginListItem> ListItems { get; }
            (StatusFlags flags, string text) Status { get; }
            ISelectedPluginData SelectedPluginData { get; }
            void OnSelect(IPluginListItem item);
            void OnAction();
        };

        internal interface IPresenterInternal : IPresenter, IViewModel { };

        [Flags]
        public enum StatusFlags
        {
            None = 0,
            IsProgressIndicatorVisible = 1,
            IsError = 2
        };

        public interface IPluginListItem : Reactive.IListItem
        {
            string Text { get; }
        };

        public interface ISelectedPluginData
        {
            string Caption { get; }
            string Description { get; }
            (bool Enabled, string Caption) ActionButton { get; }
        };

        public interface IPageAvailability
        {
            bool IsAvailable { get; }
        };
    }
};