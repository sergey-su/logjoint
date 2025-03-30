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
        IViewModel viewModel;

        public MemAndPerformanceSettingsView()
        {
            InitializeComponent();
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.SetView(this);
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

        Presenters.LabeledStepperPresenter.IView IView.GetStepperView(ViewControl ctrlId)
        {
            return GetControlById(ctrlId) as GaugeControl;
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
                case ViewControl.ClearRecentEntriesListLinkLabel: return clearRecentLogsListLinkLabel;
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
                case ViewControl.EnableAutoPostprocessingCheckBox: return enableAutoPostprocessingCheckBox;
                default: return null;
            }
        }

        private void clearRecentLogsListLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnLinkClicked(ViewControl.ClearRecentEntriesListLinkLabel);
        }

        private void clearSearchHistoryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnLinkClicked(ViewControl.ClearSearchHistoryLinkLabel);
        }

        private void clearLogSpecificStorageLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnLinkClicked(ViewControl.ClearLogSpecificStorageLinkLabel);
        }

        private void logSpecificStorageEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckboxChecked(ViewControl.LogSpecificStorageEnabledCheckBox);
        }

        private void disableMultithreadedParsingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckboxChecked(ViewControl.DisableMultithreadedParsingCheckBox);
        }

        private void enableAutoPostprocessingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckboxChecked(ViewControl.EnableAutoPostprocessingCheckBox);
        }

        private void collectUnusedMemoryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnLinkClicked(ViewControl.CollectUnusedMemoryLinkLabel);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            // this fixes the problem that on high DPIs child panels do not arrange themselves accoording to
            // their Right anchor
            var innerWidth = this.Width - this.Padding.Horizontal;
            Action<FlowLayoutPanel> fixPanel = p => { p.Width = innerWidth - p.Left; };
            fixPanel(flowLayoutPanel1);
            fixPanel(flowLayoutPanel2);
            fixPanel(flowLayoutPanel3);
            base.OnLayout(e);
        }
    }
}
