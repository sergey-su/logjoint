using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.MemAndPerformancePage;

namespace LogJoint.UI
{
	public partial class MemAndPerformanceSettingsView : UserControl, IView
	{
		IViewEvents presenter;

		public MemAndPerformanceSettingsView()
		{
			InitializeComponent();
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		bool IView.GetControlEnabled(ViewControl control)
		{
			var ctrl = GetControlById(control);
			return ctrl != null ? ctrl.Enabled : false;
		}

		void IView.SetControlEnabled(ViewControl control, bool value)
		{
			var ctrl = GetControlById(control);
			if (ctrl != null)
				ctrl.Enabled = value;
		}

		int IView.GetControlValue(ViewControl control)
		{
			var gauge = GetControlById(control) as GaugeControl;
			return gauge != null ? gauge.Value : 0;
		}

		void IView.SetControlValue(ViewControl control, int value)
		{
			var gauge = GetControlById(control) as GaugeControl;
			if (gauge != null)
				gauge.Value = value;
		}

		bool IView.GetControlChecked(ViewControl control)
		{
			var ctrl = GetControlById(control) as CheckBox;
			return ctrl != null ? ctrl.Checked : false;
		}

		void IView.SetControlChecked(ViewControl control, bool value)
		{
			var ctrl = GetControlById(control) as CheckBox;
			if (ctrl != null)
				ctrl.Checked = value;
		}

		void IView.FocusControl(ViewControl control)
		{
			var ctrl = GetControlById(control);
			if (ctrl != null && ctrl.CanFocus)
				ctrl.Focus();
		}

		bool IView.ShowConfirmationDialog(string message)
		{
			return MessageBox.Show(message, "LogJoint", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes;
		}

		void IView.SetControlText(ViewControl controlId, string value)
		{
			var ctrl = GetControlById(controlId);
			if (ctrl != null)
				ctrl.Text = value;
		}

		Control GetControlById(ViewControl id)
		{
			switch (id)
			{
				case ViewControl.RecentLogsListSizeEditor: return recentLogsListSizeEditor;
				case ViewControl.ClearRecentLogsListLinkLabel: return clearRecentLogsListLinkLabel;
				case ViewControl.SearchHistoryDepthEditor: return searchHistoryDepthEditor;
				case ViewControl.ClearSearchHistoryLinkLabel: return clearSearchHistoryLinkLabel;
				case ViewControl.MaxNumberOfSearchResultsEditor: return maxNumberOfSearchResultsEditor;
				case ViewControl.LogSpecificStorageEnabledCheckBox: return logSizeThresholdEditor;
				case ViewControl.LogSpecificStorageSpaceLimitEditor: return logSpecificStorageSpaceLimitEditor;
				case ViewControl.ClearLogSpecificStorageLinkLabel: return clearLogSpecificStorageLinkLabel;
				case ViewControl.DisableMultithreadedParsingCheckBox: return disableMultithreadedParsingCheckBox;
				case ViewControl.LogSizeThresholdEditor: return logSizeThresholdEditor;
				case ViewControl.LogWindowSizeEditor: return logWindowSizeEditor;
				case ViewControl.MemoryConsumptionLabel: return memoryConsumptionLabel;
				case ViewControl.CollectUnusedMemoryLinkLabel: return collectUnusedMemoryLinkLabel;
				default: return null;
			}
		}

		private void clearRecentLogsListLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnLinkClicked(ViewControl.ClearRecentLogsListLinkLabel);
		}

		private void clearSearchHistoryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnLinkClicked(ViewControl.ClearSearchHistoryLinkLabel);
		}

		private void clearLogSpecificStorageLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnLinkClicked(ViewControl.ClearLogSpecificStorageLinkLabel);
		}

		private void logSpecificStorageEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			presenter.OnCheckboxChecked(ViewControl.LogSpecificStorageEnabledCheckBox);
		}

		private void disableMultithreadedParsingCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			presenter.OnCheckboxChecked(ViewControl.DisableMultithreadedParsingCheckBox);
		}

		private void collectUnusedMemoryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnLinkClicked(ViewControl.CollectUnusedMemoryLinkLabel);
		}
	}
}
