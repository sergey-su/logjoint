using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.MemAndPerformancePage;

namespace LogJoint.UI
{
    public partial class MemAndPerformanceSettingsView : UserControl
    {
        IViewModel viewModel;
        ISubscription subscription;

        public MemAndPerformanceSettingsView()
        {
            InitializeComponent();
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            recentLogsListSizeEditor.SetViewModel(viewModel.RecentLogsListSizeEditor);
            searchHistoryDepthEditor.SetViewModel(viewModel.SearchHistoryDepthEditor);
            maxNumberOfSearchResultsEditor.SetViewModel(viewModel.MaxNumberOfSearchResultsEditor);
            logSizeThresholdEditor.SetViewModel(viewModel.LogSizeThresholdEditor);
            logWindowSizeEditor.SetViewModel(viewModel.LogWindowSizeEditor);

            var updateControls = Updaters.Create(
                () => viewModel.Controls,
                controls =>
                {
                    foreach (var control in controls)
                    {
                        Control ctrl = GetControlById(control.Key);
                        ctrl.Enabled = control.Value.Enabled;
                        ctrl.Text = control.Value.Text;
                        if (ctrl is CheckBox cb)
                            cb.Checked = control.Value.Checked;
                    }
                });

            subscription = viewModel.ChangeNotification.CreateSubscription(updateControls);
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

        private void disableMultithreadedParsingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckboxChecked(ViewControl.DisableMultithreadedParsingCheckBox, disableMultithreadedParsingCheckBox.Checked);
        }

        private void enableAutoPostprocessingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckboxChecked(ViewControl.EnableAutoPostprocessingCheckBox, enableAutoPostprocessingCheckBox.Checked);
        }

        private void collectUnusedMemoryLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnLinkClicked(ViewControl.CollectUnusedMemoryLinkLabel);
        }

        Control GetControlById(ViewControl id)
        {
            switch (id)
            {
                case ViewControl.ClearRecentEntriesListLinkLabel: return clearRecentLogsListLinkLabel;
                case ViewControl.ClearSearchHistoryLinkLabel: return clearSearchHistoryLinkLabel;
                case ViewControl.ClearLogSpecificStorageLinkLabel: return clearLogSpecificStorageLinkLabel;
                case ViewControl.DisableMultithreadedParsingCheckBox: return disableMultithreadedParsingCheckBox;
                case ViewControl.MemoryConsumptionLabel: return memoryConsumptionLabel;
                case ViewControl.CollectUnusedMemoryLinkLabel: return collectUnusedMemoryLinkLabel;
                case ViewControl.EnableAutoPostprocessingCheckBox: return enableAutoPostprocessingCheckBox;
                default: return null;
            }
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
