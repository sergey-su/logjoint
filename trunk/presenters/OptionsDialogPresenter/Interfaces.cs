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
			void SetPresenter(IPresenterEvents presenter);
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

		public interface IPresenterEvents
		{
			void OnLinkClicked(ViewControl control);
			void OnCheckboxChecked(ViewControl control);
		};
	};
};