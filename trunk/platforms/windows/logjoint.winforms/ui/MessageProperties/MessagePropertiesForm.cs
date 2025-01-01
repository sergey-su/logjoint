using LogJoint.UI;
using LogJoint.UI.Presenters.MessagePropertiesDialog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LogJoint
{
    public partial class MessagePropertiesForm : Form, IDialog
    {
        IDialogViewModel viewModel;
        RadioButton[] contentModeButtons;

        public MessagePropertiesForm(IDialogViewModel host)
        {
            this.viewModel = host;
            InitializeComponent();
            this.contentModeButtons = new[] { contentModeButton1, contentModeButton2, contentModeButton3 };
            InitializeTable(CreateRows());

            var tableUpdater = Updaters.Create(() => viewModel.Data, UpdateView);
            host.ChangeNotification.CreateSubscription(tableUpdater);

            FormClosed += (s, e) => host.OnClosed();

            for (var i = 0; i < contentModeButtons.Length; ++i)
            {
                var scopedIdx = i;
                contentModeButtons[i].Click += (s, e) => viewModel?.OnContentViewModeChange(scopedIdx);
            }
        }

        void IDialog.Show()
        {
            base.Show();
        }

        bool IDialog.IsDisposed
        {
            get { return base.IsDisposed; }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

        struct RowInfo
        {
            public RowStyle Style;
            public Control Ctrl1, Ctrl2;
            public RowInfo(RowStyle style, Control ctrl1, Control ctrl2)
            {
                this.Style = style;
                this.Ctrl1 = ctrl1;
                this.Ctrl2 = ctrl2;
            }
            public RowInfo(Control ctrl1, Control ctrl2) : this(new RowStyle(SizeType.AutoSize), ctrl1, ctrl2)
            {
            }
        };

        static readonly Regex SingleNRe = new Regex(@"(?<ch>[^\r])\n+", RegexOptions.ExplicitCapture);

        static string FixLineBreaks(string str)
        {
            // replace all single \n with \r\n 
            // (single \n is the \n that is not preceded by \r)
            return SingleNRe.Replace(str, "${ch}\r\n", str.Length, 0);
        }

        static Color ResolveLinkColor(LogJoint.Drawing.Color? cl)
        {
            return cl != null ? Drawing.PrimitivesExtensions.ToSystemDrawingObject(cl.Value) : SystemColors.ButtonFace;
        }

        void UpdateView(DialogData viewData, DialogData prevViewData)
        {
            timeTextBox.Text = viewData.TimeValue;

            threadLinkLabel.Text = viewData.ThreadLinkValue;
            threadLinkLabel.BackColor = ResolveLinkColor(viewData.ThreadLinkBkColor);
            threadLinkLabel.Enabled = viewData.ThreadLinkEnabled;

            logSourceLinkLabel.Text = viewData.SourceLinkValue;
            logSourceLinkLabel.BackColor = ResolveLinkColor(viewData.SourceLinkBkColor);
            logSourceLinkLabel.Enabled = viewData.SourceLinkEnabled;

            bookmarkedStatusLabel.Text = viewData.BookmarkedStatusText;
            bookmarkActionLinkLabel.Text = viewData.BookmarkActionLinkText;
            bookmarkActionLinkLabel.Enabled = viewData.BookmarkActionLinkEnabled;

            severityTextBox.Text = viewData.SeverityValue;

            messagesTextBox.Visible = viewData.TextValue != null;
            messagesTextBox.Text = FixLineBreaks(viewData.TextValue ?? "");

            var customControl = viewData.CustomView as Control;
            var prevCustomControl = prevViewData?.CustomView as Control;
            if (prevCustomControl != customControl)
            {
                if (prevCustomControl != null)
                {
                    contentsContainer.Controls.Remove(prevCustomControl);
                }
                if (customControl != null)
                {
                    contentsContainer.Controls.Add(customControl);
                    customControl.Bounds = contentsContainer.ClientRectangle;
                    customControl.Anchor = messagesTextBox.Anchor;
                }
            }

            bool hlEnabled = viewData.HighlightedCheckboxEnabled;
            nextHighlightedCheckBox.Enabled = hlEnabled;
            if (!hlEnabled)
                nextHighlightedCheckBox.Checked = false;

            for (int i = 0; i < contentModeButtons.Length; ++i)
            {
                var visible = i < viewData.ContentViewModes.Count;
                contentModeButtons[i].Visible = visible;
                contentModeButtons[i].Text = visible ? viewData.ContentViewModes[i] : "";
                contentModeButtons[i].Checked = visible && i == viewData.ContentViewModeIndex;
            }
        }

        List<RowInfo> CreateRows()
        {
            List<RowInfo> rows = new List<RowInfo>();

            rows.Add(new RowInfo(timeLabel, timeTextBox));
            rows.Add(new RowInfo(threadLabel, threadLinkLabel));
            rows.Add(new RowInfo(logSourceLabel, logSourceLinkLabel));
            rows.Add(new RowInfo(bookmarkedLabel, bookmarkValuePanel));
            rows.Add(new RowInfo(severityLabel, severityTextBox));
            rows.Add(new RowInfo(contentLabel, contentModesFlowLayoutPanel));
            rows.Add(new RowInfo(new RowStyle(SizeType.Percent, 100), contentsContainer, null));

            return rows;
        }

        void InitializeTable(List<RowInfo> rows)
        {
            TableLayoutPanel tbl = tableLayoutPanel1;

            tbl.SuspendLayout();

            tbl.Controls.Clear();
            tbl.RowCount = rows.Count;
            while (tbl.RowStyles.Count < rows.Count)
                tbl.RowStyles.Add(new RowStyle());

            int tabIdx = 50;

            for (int i = 0; i < rows.Count; ++i)
            {
                RowInfo r = rows[i];

                tbl.RowStyles[i] = r.Style;

                tbl.Controls.Add(r.Ctrl1);
                tbl.SetCellPosition(r.Ctrl1, new TableLayoutPanelCellPosition(0, i));

                if (r.Ctrl2 != null)
                {
                    tbl.Controls.Add(r.Ctrl2);
                    tbl.SetCellPosition(r.Ctrl2, new TableLayoutPanelCellPosition(1, i));
                    r.Ctrl2.Dock = DockStyle.Fill;
                }
                else
                {
                    tbl.SetColumnSpan(r.Ctrl1, 2);
                    r.Ctrl1.Dock = DockStyle.Fill;
                }

                if (r.Ctrl1 != null)
                    r.Ctrl1.TabIndex = ++tabIdx;
                if (r.Ctrl2 != null)
                    r.Ctrl2.TabIndex = ++tabIdx;
            }

            tbl.ResumeLayout(true);
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void threadLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnThreadLinkClicked();
        }

        private void logSourceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnSourceLinkClicked();
        }

        private void bookmarkActionLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnBookmarkActionClicked();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void prevLineButton_Click(object sender, EventArgs e)
        {
            viewModel.OnPrevClicked(nextHighlightedCheckBox.Checked);
        }

        private void nextLineButton_Click(object sender, EventArgs e)
        {
            viewModel.OnNextClicked(nextHighlightedCheckBox.Checked);
        }

        private const int EM_SETTABSTOPS = 0x00CB;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);

        private void MessagePropertiesForm_Load(object sender, EventArgs e)
        {
            SendMessage(messagesTextBox.Handle, EM_SETTABSTOPS, 1, new int[] { 7 });
        }
    }

    class MessagePropertiesDialogView : IView
    {
        private readonly IWinFormsComponentsInitializer formsInitializer;

        public MessagePropertiesDialogView(IWinFormsComponentsInitializer formsInitializer)
        {
            this.formsInitializer = formsInitializer;
        }

        IDialog IView.CreateDialog(IDialogViewModel model)
        {
            MessagePropertiesForm frm = new MessagePropertiesForm(model);
            formsInitializer.InitOwnedForm(frm);
            return frm;
        }
    };
}