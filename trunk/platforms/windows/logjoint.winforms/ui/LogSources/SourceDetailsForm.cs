using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using System.Threading.Tasks;

namespace LogJoint.UI
{
    public partial class SourceDetailsForm : Form, IWindow
    {
        readonly IViewModel viewModel;

        public SourceDetailsForm(IViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;

            var updateControls = Updaters.Create(
                () => viewModel.ViewState,
                UpdateControls
            );

            viewModel.ChangeNotification.CreateSubscription(updateControls);
        }

        Task IWindow.ShowModalDialog()
        {
            if (!IsDisposed)
                this.ShowDialog();
            return Task.FromResult(0);
        }

        void UpdateControls(IViewState state)
        {
            void updateControl(
                Control ctrl,
                ControlState controlState,
                TextBoxBase textBox = null,
                bool backColor = false,
                bool foreColor = false
            )
            {
                ctrl.Visible = !controlState.Hidden;
                ctrl.Enabled = !controlState.Disabled;
                if (backColor)
                    ctrl.BackColor = controlState.BackColor != null ? controlState.BackColor.Value.ToSystemDrawingObject() : System.Drawing.SystemColors.Control;
                if (foreColor)
                    ctrl.ForeColor = controlState.ForeColor != null ? controlState.ForeColor.Value.ToSystemDrawingObject() : System.Drawing.SystemColors.ControlText;
                if (ctrl.Text != controlState.Text)
                {
                    ctrl.Text = controlState.Text;
                    textBox?.Select(0, 0);
                }
                toolTip1.SetToolTip(ctrl, controlState.Tooltip);
            }

            void updateCheckBox(CheckBox control, ControlState controlState)
            {
                updateControl(control, controlState);
                control.Checked = controlState.Checked.GetValueOrDefault();
            }

            void updateTextBox(TextBox control, ControlState controlState)
            {
                updateControl(control, controlState, control);
            }

            updateTextBox(nameTextBox, state.NameEditbox);
            updateTextBox(formatTextBox, state.FormatTextBox);
            updateCheckBox(visibleCheckBox, state.VisibleCheckBox);
            updateControl(colorPanel, state.ColorPanel, backColor: true);
            updateControl(stateDetailsLink, state.StateDetailsLink);
            updateControl(stateLabel, state.StateLabel, foreColor: true);
            updateTextBox(loadedMessagesTextBox, state.LoadedMessagesTextBox);
            updateControl(loadedMessagesWarningIcon, state.LoadedMessagesWarningIcon);
            updateControl(loadedMessagesWarningLinkLabel, state.LoadedMessagesWarningLinkLabel);
            updateControl(trackChangesLabel, state.TrackChangesLabel);
            updateControl(suspendResumeTrackingLink, state.SuspendResumeTrackingLink);
            updateControl(firstMessageLinkLabel, state.FirstMessageLinkLabel);
            updateControl(lastMessageLinkLabel, state.LastMessageLinkLabel);
            updateControl(saveAsButton, state.SaveAsButton);
            updateTextBox(annotationTextBox, state.AnnotationTextBox);
            updateTextBox(timeOffsetTextBox, state.TimeOffsetTextBox);
            updateControl(copyPathLink, state.CopyPathButton);
        }

        void IWindow.ShowColorSelector(Color[] options)
        {
            var menu = new ContextMenuStrip();
            foreach (var cl in options)
            {
                var mi = new ToolStripMenuItem()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.None,
                    BackColor = cl.ToSystemDrawingObject(),
                    AutoSize = false,
                    Size = new System.Drawing.Size(300, (int)UIUtils.Dpi.Scale(15f))
                };
                mi.Paint += colorOptionMenuItemPaint;
                mi.Click += (s, e) => viewModel.OnColorSelected(cl);
                menu.Items.Add(mi);
            }
            menu.Show(changeColorLinkLabel, new System.Drawing.Point(0, changeColorLinkLabel.Height));
        }

        private void colorOptionMenuItemPaint(object sender, PaintEventArgs e)
        {
            var mi = sender as ToolStripMenuItem;
            if (mi == null)
                return;
            using (var b = new System.Drawing.SolidBrush(mi.BackColor))
                e.Graphics.FillRectangle(b, e.ClipRectangle);
            e.Graphics.DrawLine(System.Drawing.Pens.LightGray, 0, 0, e.ClipRectangle.Right, 0);
        }

        private void visibleCheckBox_Click(object sender, EventArgs e)
        {
            viewModel.OnVisibleCheckBoxChange(visibleCheckBox.Checked);
        }

        private void suspendResumeTrackingLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnSuspendResumeTrackingLinkClicked();
        }

        private void stateDetailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnStateDetailsLinkClicked();
        }

        private void lastBookmarkClicked(object sender, EventArgs e)
        {
            viewModel.OnLastKnownMessageLinkClicked();
        }

        private void firstBookmarkClicked(object sender, EventArgs e)
        {
            viewModel.OnFirstKnownMessageLinkClicked();
        }

        private void saveAsButton_Click(object sender, EventArgs e)
        {
            viewModel.OnSaveAsButtonClicked();
        }

        private void SourceDetailsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            viewModel.OnClosingDialog();
        }

        private void loadedMessagesWarningIcon_Click(object sender, EventArgs e)
        {
            viewModel.OnLoadedMessagesWarningIconClicked();
        }

        void changeColorLinkLabel_Click(object sender, System.EventArgs e)
        {
            viewModel.OnChangeColorLinkClicked();
        }

        void copyPathLink_LinkClicked(object sender, System.EventArgs e)
        {
            viewModel.OnCopyButtonClicked();
        }

        void annotationTextBox_TextChanged(object sender, System.EventArgs e)
        {
            viewModel.OnChangeAnnotation(annotationTextBox.Text);
        }

        void timeOffsetTextBox_TextChanged(object sender, System.EventArgs e)
        {
            viewModel.OnChangeChangeTimeOffset(timeOffsetTextBox.Text);
        }

        void changeColorLinkLabel_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnChangeColorLinkClicked();
        }
    }

    public class SourceDetailsWindowView : IView
    {
        IViewModel viewModel;

        void IView.SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        IWindow IView.CreateWindow()
        {
            return new SourceDetailsForm(viewModel);
        }
    };
}