using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FilterDialog;
using LogJoint.Drawing;
using LogJoint.UI.Windows.Reactive;
using System.Collections.Immutable;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LogJoint.UI
{
    public partial class FilterDialog : Form, IView
    {
        public IViewModel viewModel;
        public KeyValuePair<string, Color?>[] actionComboBoxOptions;
        readonly ISubscription subscription;

        public FilterDialog(IViewModel viewModel, IReactive reactive)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            viewModel.SetView(this);
            var updateConfig = Updaters.Create(() => viewModel.Config, config =>
            {
                Text = config.Title;
            });
            var scopesListController = reactive.CreateListBoxController<IScopeItem>(threadsCheckedListBox);
            scopesListController.Stringifier = (IScopeItem item) => new string(' ', item.Indent * 4) + item.ToString();
            scopesListController.OnUpdateRow = (item, i, oldItem) => threadsCheckedListBox.SetItemChecked(i, item.IsChecked);
            threadsCheckedListBox.ItemCheck += (s, e) =>
            {
                if (!scopesListController.IsUpdating)
                    viewModel.OnScopeItemCheck(scopesListController.Map(threadsCheckedListBox.Items[e.Index]), e.NewValue == CheckState.Checked);
            };
            scopesListController.OnSelect = s => viewModel.OnScopeItemSelect(s.LastOrDefault());
            var updateScopes = Updaters.Create(() => viewModel.ScopeItems, scopeItems =>
            {
                scopeNotSupportedLabel.Visible = scopeItems == null;
                scopesListController.Update(scopeItems ?? ImmutableList<IScopeItem>.Empty);
            });

            var messageTypesListController = reactive.CreateListBoxController<IMessageTypeItem>(messagesTypesCheckedListBox);
            messageTypesListController.OnUpdateRow = (item, i, oldItem) => messagesTypesCheckedListBox.SetItemChecked(i, item.IsChecked);
            messagesTypesCheckedListBox.ItemCheck += (s, e) =>
            {
                if (!messageTypesListController.IsUpdating)
                    viewModel.OnMessageTypeItemCheck(messageTypesListController.Map(messagesTypesCheckedListBox.Items[e.Index]), e.NewValue == CheckState.Checked);
            };
            messageTypesListController.OnSelect = s => viewModel.OnMessageTypeItemSelect(s.LastOrDefault());
            var updateMessageTypes = Updaters.Create(() => viewModel.MessageTypeItems, messageTypesListController.Update);

            var updateCriteria = Updaters.Create(() => viewModel.CheckedBoxes, () => viewModel.Template,
                (cbs, template) =>
            {
                enabledCheckBox.Checked = (cbs & CheckBoxId.FilterEnabled) != 0;
                matchCaseCheckbox.Checked = (cbs & CheckBoxId.MatchCase) != 0;
                regExpCheckBox.Checked = (cbs & CheckBoxId.RegExp) != 0;
                wholeWordCheckbox.Checked = (cbs & CheckBoxId.WholeWord) != 0;
                bool lengthChanged = templateTextBox.Text.Length != template?.Length;
                templateTextBox.Text = template;
                if (lengthChanged)
                    templateTextBox.Select(templateTextBox.Text.Length, 0);
            });

            var updateAction = Updaters.Create(() => viewModel.Config, () => viewModel.ActionComboBoxValue, (config, actionComboBoxValue) =>
            {
                actionComboBoxOptions = config.ActionComboBoxOptions;
                actionComboBox.Items.Clear();
                actionComboBox.Items.AddRange(actionComboBoxOptions.Select(a => a.Key).ToArray());
                actionComboBox.SelectedIndex = actionComboBoxValue;
            });

            var updateNameEdit = Updaters.Create(() => viewModel.NameEdit, props =>
            {
                nameTextBox.Text = props.Value;
                nameTextBox.Enabled = props.Enabled;
                nameLinkLabel.Text = props.LinkText;
            });

            var updateTimeRange = Updaters.Create(() => viewModel.BeginTimeBound, () => viewModel.EndTimeBound, (begin, end) =>
            {
                void updateBoundControls(TimeRangeBoundProperties properties, CheckBox cb, DateTimePicker picker, LinkLabel setCurrent)
                {
                    cb.Checked = properties.Enabled;
                    picker.Value = properties.Value;
                    picker.Enabled = properties.Enabled;
                    setCurrent.Enabled = properties.SetCurrentLinkEnabled;
                    setCurrent.Text = properties.SetCurrentLinkName;
                    toolTip.SetToolTip(setCurrent, properties.SetCurrentLinkHint);
                }
                updateBoundControls(begin, timeRangeBeginCheckBox, timeRangeBeginPicker, timeRangeBeginSetCurrentLink);
                updateBoundControls(end, timeRangeEndCheckBox, timeRangeEndPicker, timeRangeEndSetCurrentLink);
            });
            timeRangeBeginCheckBox.Click += (sender, evt) => viewModel.OnTimeBoundEnabledChange(TimeBound.Begin, timeRangeBeginCheckBox.Checked);
            timeRangeBeginPicker.ValueChanged += (sender, evt) => viewModel.OnTimeBoundValueChanged(TimeBound.Begin, timeRangeBeginPicker.Value);
            timeRangeBeginSetCurrentLink.Click += (sender, evt) => viewModel.OnSetCurrentTimeClicked(TimeBound.Begin);
            timeRangeEndCheckBox.Click += (sender, evt) => viewModel.OnTimeBoundEnabledChange(TimeBound.End, timeRangeEndCheckBox.Checked);
            timeRangeEndPicker.ValueChanged += (sender, evt) => viewModel.OnTimeBoundValueChanged(TimeBound.End, timeRangeEndPicker.Value);
            timeRangeEndSetCurrentLink.Click += (sender, evt) => viewModel.OnSetCurrentTimeClicked(TimeBound.End);

            var dialogConroller = new ModalDialogController(this);
            var showHide = Updaters.Create(() => viewModel.IsVisible, dialogConroller.SetVisibility);

            this.subscription = viewModel.ChangeNotification.CreateSubscription(() =>
            {
                updateConfig();
                updateScopes();
                updateMessageTypes();
                updateCriteria();
                updateAction();
                updateNameEdit();
                showHide();
                updateTimeRange();
            });
        }

        void IView.PutFocusOnNameEdit()
        {
            if (templateTextBox.CanFocus)
                templateTextBox.Focus();
        }

        private void FilterDialog_Shown(object sender, EventArgs e)
        {
            templateTextBox.Focus();
        }

        private void HandleCheckBoxChanged(object sender, EventArgs e)
        {
            viewModel.OnCheckBoxCheck(CheckBoxId.MatchCase, matchCaseCheckbox.Checked);
            viewModel.OnCheckBoxCheck(CheckBoxId.RegExp, regExpCheckBox.Checked);
            viewModel.OnCheckBoxCheck(CheckBoxId.WholeWord, wholeWordCheckbox.Checked);
            viewModel.OnCheckBoxCheck(CheckBoxId.FilterEnabled, enabledCheckBox.Checked);
        }

        private void HandleTemplateChanged(object sender, EventArgs e)
        {
            viewModel.OnTemplateChange(templateTextBox.Text);
        }

        private void HandleNameChanged(object sender, EventArgs e)
        {
            viewModel.OnNameChange(nameTextBox.Text);
        }

        private void ActionComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            var option = actionComboBoxOptions.ElementAtOrDefault(e.Index);
            if (option.Value != null)
            {
                using (var b = new System.Drawing.SolidBrush(option.Value.Value.ToSystemDrawingObject()))
                {
                    e.Graphics.FillRectangle(b, e.Bounds);
                }
                if ((e.State & DrawItemState.Selected) != 0)
                {
                    var r = e.Bounds;
                    r.Width = 3;
                    e.Graphics.FillRectangle(System.Drawing.Brushes.Blue, r);
                    r.X = e.Bounds.Right - r.Width;
                    e.Graphics.FillRectangle(System.Drawing.Brushes.Blue, r);
                }
                e.Graphics.DrawString(option.Key, e.Font, System.Drawing.Brushes.Black, e.Bounds);
            }
            else
            {
                e.DrawBackground();
                using (var b = new System.Drawing.SolidBrush(e.ForeColor))
                    e.Graphics.DrawString(option.Key ?? "", e.Font, b, e.Bounds);
            }
        }

        private void nameLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnNameEditLinkClicked();
        }

        private void FilterDialog_Closing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            viewModel.OnCancelled();
        }

        private void OkButton_Click(object sender, System.EventArgs e)
        {
            viewModel.OnConfirmed();
        }

        private void ActionComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            viewModel.OnActionComboBoxValueChange(actionComboBox.SelectedIndex);
        }

        private void TabPage1_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
        {
            // Manipulating the size manually because Anchor-ing on this tab page does not work
            // correctly for some reason.
            threadsContainer.Width = tabPage1.Width;
            threadsContainer.Height = tabPage1.Height - threadsContainer.Top;
        }
    }
}