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
			int GetControlValue(ViewControl control);
			void SetControlValue(ViewControl control, int value);
			bool GetControlChecked(ViewControl control);
			void SetControlChecked(ViewControl control, bool value);
			void FocusControl(ViewControl control);
			bool ShowConfirmationDialog(string message);
			void SetControlText(ViewControl ctrlId, string value);
		};

		public enum ViewControl
		{
			RecentLogsListSizeEditor,
			ClearRecentLogsListLinkLabel,
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
			CollectUnusedMemoryLinkLabel
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
			void SetPresenter(IViewEvents presenter);
			void SetControlChecked(ViewControl control, bool value);
			bool GetControlChecked(ViewControl control);
			void SetFontFamiliesControl(string[] options, int selectedOption);
			void SetFontSizeControl(int[] options, int currentValue);
			int GetSelectedFontFamily();
			int GetFontSizeControlValue();
		};

		public enum ViewControl
		{
			ColoringNoneRadioButton,
			ColoringThreadsRadioButton,
			ColoringSourcesRadioButton,
		};

		public interface IViewEvents
		{
			void OnRadioButtonChecked(ViewControl control);
			void OnSelectedFontChanged();
			void OnFontSizeValueChanged();
		};
	};
};