using System;
using System.Collections.Generic;

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
            IDialog CreateDialog();
        };


        public interface IDialog : IDisposable
        {
            void SetViewModel(IDialogViewModel viewModel);
            MemAndPerformancePage.IView MemAndPerformancePage { get; }
            Appearance.IView ApperancePage { get; }
            UpdatesAndFeedback.IView UpdatesAndFeedbackPage { get; }
            Plugins.IView PluginsPage { get; }
            void Show(PageId? initiallySelectedPage);
            void Hide();
        };

        public interface IDialogViewModel
        {
            void OnOkPressed();
            void OnCancelPressed();
            PageId VisiblePages { get; }
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

        public interface IView
        {
            void SetPresenter(IViewEvents presenter);
            bool GetControlEnabled(ViewControl control);
            void SetControlEnabled(ViewControl control, bool value);
            bool GetControlChecked(ViewControl control);
            void SetControlChecked(ViewControl control, bool value);
            void FocusControl(ViewControl control);
            bool ShowConfirmationDialog(string message);
            void SetControlText(ViewControl ctrlId, string value);
            LabeledStepperPresenter.IView GetStepperView(ViewControl ctrlId);
        };

        public enum ViewControl
        {
            RecentLogsListSizeEditor,
            ClearRecentEntriesListLinkLabel,
            SearchHistoryDepthEditor,
            ClearSearchHistoryLinkLabel,
            MaxNumberOfSearchResultsEditor,
            LogSpecificStorageEnabledCheckBox,
            LogSpecificStorageSpaceLimitEditor,
            ClearLogSpecificStorageLinkLabel,
            DisableMultithreadedParsingCheckBox,
            LogSizeThresholdEditor,
            LogWindowSizeEditor,
            MemoryConsumptionLabel,
            CollectUnusedMemoryLinkLabel,
            EnableAutoPostprocessingCheckBox,
        };

        public interface IViewEvents
        {
            void OnLinkClicked(ViewControl control);
            void OnCheckboxChecked(ViewControl control);
        };
    };

    namespace Appearance
    {
        public interface IPresenter : IDisposable
        {
            bool Apply();
        };

        public interface IView
        {
            LogViewer.IView PreviewLogView { get; }
            LabeledStepperPresenter.IView FontSizeControlView { get; }
            void SetPresenter(IViewEvents presenter); // todo: remove
            void SetSelectorControl(ViewControl selector, string[] options, int selectedOption);
            int GetSelectedValue(ViewControl selector);
        };

        public enum ViewControl
        {
            ColoringSelector,
            FontFamilySelector,
            PaletteSelector
        };

        public interface IViewEvents
        {
            void OnSelectedValueChanged(ViewControl ctrl);
        };
    };

    namespace UpdatesAndFeedback
    {
        public interface IPresenter
        {
            bool IsAvailable { get; }
            bool Apply();
        };

        public interface IView
        {
            void SetPresenter(IViewEvents presenter);
            void SetLastUpdateCheckInfo(string brief, string details);
            void SetCheckNowButtonAvailability(bool canCheckNow);
        };

        public interface IViewEvents
        {
            void OnCheckUpdateNowClicked();
        };
    };

    namespace Plugins
    {
        public interface IPresenter : IDisposable, IPageAvailability
        {
            bool Apply();
        };

        public interface IView
        {
            void SetViewModel(IViewModel viewModel);
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