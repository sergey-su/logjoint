using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.Options
{
	namespace Dialog
	{
		public interface IPresenter
		{
			void ShowDialog();
		};

		public interface IView
		{
			void SetPresenter(IPresenterEvents presenter);
			IDialog CreateDialog();
		};


		public interface IDialog: IDisposable
		{
			MemAndPerformancePage.IView MemAndPerformancePage { get; }
			Appearance.IView ApperancePage { get; }
			UpdatesAndFeedback.IView UpdatesAndFeedbackPage { get; }
			void SetUpdatesAndFeedbackPageVisibility(bool value);
			void Show();
			void Hide();
		};

		public interface IPresenterEvents
		{
			void OnOkPressed();
			void OnCancelPressed();
		};

		public interface IPagePresenter
		{
			void Apply();
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
		public interface IPresenter
		{
			bool Apply();
		};

		public interface IView
		{
			LogViewer.IView PreviewLogView { get; }
			LabeledStepperPresenter.IView FontSizeControlView { get; }
			void SetPresenter(IViewEvents presenter);
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
};