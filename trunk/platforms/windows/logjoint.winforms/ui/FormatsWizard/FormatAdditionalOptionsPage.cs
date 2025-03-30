using System;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.FormatAdditionalOptionsPage;

namespace LogJoint.UI
{
    public partial class FormatAdditionalOptionsPage : UserControl, IView
    {
        IViewModel viewModel;

        public FormatAdditionalOptionsPage()
        {
            InitializeComponent();
        }

        private void extensionTextBox_TextChanged(object sender, EventArgs e)
        {
            viewModel.OnExtensionTextBoxChanged();
        }

        private void extensionsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewModel.OnExtensionsListBoxSelectionChanged();
        }

        private void addExtensionButton_Click(object sender, EventArgs e)
        {
            viewModel.OnAddExtensionClicked();
        }

        private void removeExtensionButton_Click(object sender, EventArgs e)
        {
            viewModel.OnDelExtensionClicked();
        }

        private void enableDejitterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnEnableDejitterCheckBoxClicked();
        }

        private void dejitterHelpLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnDejitterHelpLinkClicked();
        }

        int IView.EncodingComboBoxSelection
        {
            get { return encodingComboBox.SelectedIndex; }
            set { encodingComboBox.SelectedIndex = value; }
        }

        bool IView.EnableDejitterCheckBoxChecked
        {
            get { return enableDejitterCheckBox.Checked; }
            set { enableDejitterCheckBox.Checked = value; }
        }

        string IView.ExtensionTextBoxValue
        {
            get { return extensionTextBox.Text; }
            set { extensionTextBox.Text = value; }
        }

        void IView.SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            dejitterBufferSizeGauge.SetViewModel(viewModel.BufferStepper);
        }

        void IView.SetPatternsListBoxItems(string[] value)
        {
            patternsListBox.Items.Clear();
            patternsListBox.Items.AddRange(value);
        }

        int[] IView.GetPatternsListBoxSelection()
        {
            return patternsListBox.SelectedIndices.OfType<int>().ToArray();
        }

        void IView.SetEncodingComboBoxItems(string[] items)
        {
            encodingComboBox.Items.Clear();
            encodingComboBox.Items.AddRange(items);
        }

        void IView.EnableControls(bool addExtensionButton, bool removeExtensionButton)
        {
            this.addExtensionButton.Enabled = addExtensionButton;
            this.removeExtensionButton.Enabled = removeExtensionButton;
        }
    }
}
