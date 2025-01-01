using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.TagsList;

namespace LogJoint.UI
{
    public partial class AllTagsDialog : Form, IDialogView
    {
        readonly IDialogViewModel viewModel;
        readonly ISubscription subscription;
        readonly Action detectFormulaCursorPositionChange;
        bool wasOpen;

        public AllTagsDialog(
            IChangeNotification changeNotification,
            IDialogViewModel viewModel,
            IEnumerable<string> availableTags,
            string initiallyFocusedTag
        )
        {
            this.viewModel = viewModel;
            InitializeComponent();

            checkedListBox1.BeginUpdate();
            int focusedTagIndex = -1;
            foreach (var t in availableTags)
            {
                var idx = checkedListBox1.Items.Add(t);
                if (initiallyFocusedTag != null && t == initiallyFocusedTag)
                    focusedTagIndex = idx;
            }
            checkedListBox1.EndUpdate();
            if (focusedTagIndex >= 0)
            {
                checkedListBox1.SelectedIndex = focusedTagIndex;
                checkedListBox1.TopIndex = focusedTagIndex;
            }

            var listUpdater = Updaters.Create(
                () => viewModel.SelectedTags,
                () => viewModel.IsEditingFormula,
                (selected, editingFormula) =>
                {
                    checkedListBox1.Tag = "ignore events";
                    checkedListBox1.BeginUpdate();
                    foreach (var t in availableTags.ZipWithIndex())
                        checkedListBox1.SetItemChecked(t.Key, selected.Contains(t.Value));
                    checkedListBox1.Enabled = !editingFormula;
                    checkedListBox1.EndUpdate();
                    checkedListBox1.Tag = null;
                }
            );
            Color getLinkColor(MessageSeverity sev) =>
                sev == MessageSeverity.Error ? Color.Red :
                sev == MessageSeverity.Warning ? Color.DarkOrange :
                SystemColors.WindowText;
            var formulaUpdater = Updaters.Create(
                () => viewModel.Formula,
                () => viewModel.IsEditingFormula,
                () => viewModel.FormulaStatus,
                (formula, editing, status) =>
                {
                    formulaTextBox.Text = formula;
                    formulaTextBox.ReadOnly = !editing;
                    checkAllLinkLabel.Enabled = !editing;
                    checkNoneLinkLabel.Enabled = !editing;
                    okButton.Enabled = !editing;
                    formulaLinkLabel.Text = editing ? "done" : "edit";
                    var (statusText, statusSeverity) = status;
                    UIUtils.SetLinkContents(formulaStatusLinkLabel, statusText);
                    formulaLinkLabel.Enabled = statusSeverity != MessageSeverity.Error;
                    formulaStatusLinkLabel.ForeColor = getLinkColor(statusSeverity);
                }
            );
            var formulaFocusSideEffect = Updaters.Create(
                () => viewModel.IsEditingFormula,
                editing =>
                {
                    if (editing && formulaTextBox.CanFocus)
                        formulaTextBox.Focus();
                }
            );
            var updateSuggestions = Updaters.Create(
                () => viewModel.FormulaSuggesions,
                value =>
                {
                    var (list, selectedItem) = value;
                    suggestionsPanel.Visible = !list.IsEmpty;
                    suggestionsPanel.Controls.Clear();
                    suggestionsPanel.Controls.AddRange(list.Select((str, idx) =>
                    {
                        var lbl = new Label()
                        {
                            Text = str,
                            AutoSize = true,
                            Left = 4,
                            Top = 2 + idx * (formulaLinkLabel.Height + 3),
                            ForeColor = idx == selectedItem ? SystemColors.HighlightText : SystemColors.ControlText,
                            BackColor = idx == selectedItem ? SystemColors.Highlight : suggestionsPanel.BackColor
                        };
                        lbl.MouseDown += (s, e) => viewModel.OnSuggestionClicked(idx);
                        return lbl;
                    }).ToArray());
                    if (selectedItem != null)
                        suggestionsPanel.ScrollControlIntoView(suggestionsPanel.Controls[selectedItem.Value]);
                }
            );
            var listStatusUpdater = Updaters.Create(
                () => viewModel.TagsListStatus,
                (status) =>
                {
                    var (statusText, statusSeverity) = status;
                    UIUtils.SetLinkContents(tagsStatusLinkLabel, statusText);
                    tagsStatusLinkLabel.ForeColor = getLinkColor(statusSeverity);
                }
            );
            subscription = changeNotification.CreateSubscription(() =>
            {
                listUpdater();
                formulaUpdater();
                formulaFocusSideEffect();
                updateSuggestions();
                listStatusUpdater();
            });

            detectFormulaCursorPositionChange = Updaters.Create(
                () => formulaTextBox.SelectionStart,
                _ => changeNotification.Post()
            );

            checkedListBox1.ItemCheck += (sender, e) =>
            {
                if (checkedListBox1.Tag != null)
                    return;
                var tag = (string)checkedListBox1.Items[e.Index];
                if (e.NewValue == CheckState.Checked)
                    viewModel.OnUseTagClicked(tag);
                else
                    viewModel.OnUnuseTagClicked(tag);
            };
            cancelButton.Click += (sender, e) => viewModel.OnCancelDialog();
            okButton.Click += (sender, e) => viewModel.OnConfirmDialog();
        }

        void IDialogView.Close()
        {
            base.Close();
        }

        void IDialogView.Open()
        {
            if (wasOpen)
                throw new InvalidOperationException("can not Open same dialog twice");
            wasOpen = true;
            using (this)
            using (subscription)
            {
                ShowDialog();
            }
        }

        int IDialogView.FormulaCursorPosition
        {
            get => formulaTextBox.SelectionStart;
            set
            {
                subscription.SideEffect();
                formulaTextBox.Select(value, 0);
                if (formulaTextBox.CanFocus)
                    formulaTextBox.Focus();
            }
        }

        void IDialogView.OpenFormulaTab()
        {
            tabControl.SelectTab(formulaTabPage);
        }

        private void checkAllLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender == checkAllLinkLabel)
                viewModel.OnUseAllClicked();
            else
                viewModel.OnUnuseAllClicked();
        }

        private void formulaTextBox_TextChanged(object sender, EventArgs e)
        {
            if (viewModel.IsEditingFormula) // ignore when called when form is loaded
                viewModel.OnFormulaChange(formulaTextBox.Text);
        }

        private void formulaLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (viewModel.IsEditingFormula)
                viewModel.OnStopEditingFormulaClicked();
            else
                viewModel.OnEditFormulaClicked();
        }

        private void formulaErrorLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnFormulaLinkClicked(e.Link.LinkData as string);
        }

        private void formulaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            KeyCode k = KeyCode.None;
            if (e.KeyCode == Keys.Up)
                k = KeyCode.Up;
            else if (e.KeyCode == Keys.Down)
                k = KeyCode.Down;
            if (k != KeyCode.None)
            {
                e.Handled = viewModel.OnFormulaKeyPressed(k);
            }
        }

        private void formulaTextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            KeyCode k = KeyCode.None;
            if (e.KeyCode == Keys.Enter)
                k = KeyCode.Enter;
            if (k != KeyCode.None)
            {
                viewModel.OnFormulaKeyPressed(k);
            }
        }

        private void formulaCursorPositionTimer_Tick(object sender, EventArgs e)
        {
            // update by timer. there is no event for that :(
            detectFormulaCursorPositionChange();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnTagsStatusLinkClicked(e.Link.LinkData as string);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                viewModel.OnCancelDialog();
                return false;
            }
            else
            {
                return base.ProcessDialogKey(keyData);
            }
        }
    }
}
