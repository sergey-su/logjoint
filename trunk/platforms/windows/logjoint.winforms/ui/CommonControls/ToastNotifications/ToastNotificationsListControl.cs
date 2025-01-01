using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using LogJoint.UI;
using System.Collections.Generic;

namespace LogJoint.UI
{
    public partial class ToastNotificationsListControl : UserControl
    {
        IViewModel viewModel;
        bool ctrlsInitialzed;

        public ToastNotificationsListControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            var updateVisibility = Updaters.Create(() => viewModel.Visible, value => this.Visible = value);
            var updateItems = Updaters.Create(() => viewModel.Items, Update);

            viewModel.ChangeNotification.CreateSubscription(() =>
            {
                updateVisibility();
                updateItems();
            });
        }

        void Update(IReadOnlyList<ViewItem> items)
        {
            var allCtrls = new[]
            {
                new { l = linkLabel1, p = progressBar1, b = button1 },
                new { l = linkLabel2, p = progressBar2, b = button2 },
                new { l = linkLabel3, p = progressBar3, b = button3 },
                new { l = linkLabel4, p = progressBar4, b = button4 },
            };
            foreach (var x in allCtrls.ZipWithIndex())
            {
                var data = x.Key < items.Count ? items[x.Key] : null;
                var ctrls = allCtrls[x.Key];
                ctrls.l.Visible = data != null;
                ctrls.p.Visible = data != null && data.Progress != null;
                ctrls.b.Visible = data != null && data.IsSuppressable;
                if (data != null)
                {
                    UIUtils.SetLinkContents(ctrls.l, data.Contents);
                    ctrls.p.Value = (int)(data.Progress.GetValueOrDefault(0d) * 100d);
                }
                ctrls.l.Tag = data;
                ctrls.b.Tag = data;
                if (!ctrlsInitialzed)
                {
                    ctrls.l.LinkClicked += linkClicked;
                    ctrls.b.Click += suppressButtonClicked;
                }
            }
            ctrlsInitialzed = true;
        }

        void suppressButtonClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Tag is ViewItem)
                viewModel.OnItemSuppressButtonClicked((ViewItem)btn.Tag);
        }

        void linkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var ctrl = sender as Control;
            if (ctrl != null && e.Link.LinkData is string)
                viewModel.OnItemActionClicked((ViewItem)ctrl.Tag, (string)e.Link.LinkData);
        }
    }
}
