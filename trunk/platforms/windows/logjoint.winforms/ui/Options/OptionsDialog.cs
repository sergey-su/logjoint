using LogJoint.UI.Presenters.Options.Dialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
    public partial class OptionsDialog : Form, IDialog
    {
        IDialogViewModel viewModel;
        readonly Dictionary<PageId, TabPage> pages;

        public OptionsDialog(Windows.Reactive.IReactive reactive, IDialogViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            appearanceSettingsView1.SetViewModel(viewModel.AppearancePage);
            memAndPerformanceSettingsView.SetViewModel(viewModel.MemAndPerformancePage);
            updatesAndFeedbackView1.SetViewModel(viewModel.UpdatesAndFeedbackPage);
            pluginsView1.Init(viewModel.PluginsPage, reactive);
            pages = new Dictionary<PageId, TabPage>
            {
                { PageId.Plugins, pluginsTabPage },
                { PageId.UpdatesAndFeedback, updatesAndFeedbackTabPage },
                { PageId.Appearance, appearanceTabPage },
                { PageId.MemAndPerformance, memAndPerformanceTabPage },
            };
        }

        void IDialog.Show(PageId? initiallySelectedPage)
        {
            foreach (var page in pages)
                SetPageVisibility((viewModel.VisiblePages & page.Key) != 0, page.Value);
            if (initiallySelectedPage.HasValue && pages.TryGetValue(initiallySelectedPage.Value, out var p))
                tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(p);
            this.ShowDialog();
        }

        void IDialog.Hide()
        {
            DialogResult = DialogResult.OK;
        }

        void SetPageVisibility(bool value, TabPage page)
        {
            if (value == (tabControl1.TabPages.IndexOf(page) >= 0))
                return;
            if (!value)
                tabControl1.TabPages.Remove(page);
            else
                tabControl1.TabPages.Add(page);
        }

        void IDisposable.Dispose()
        {
            base.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            viewModel.OnOkPressed();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            viewModel.OnCancelPressed();
        }
    }

    public class OptionsDialogView : IView
    {
        readonly Windows.Reactive.IReactive reactive;

        public OptionsDialogView(Windows.Reactive.IReactive reactive)
        {
            this.reactive = reactive;
        }

        IDialog IView.CreateDialog(IDialogViewModel viewModel)
        {
            return new OptionsDialog(reactive, viewModel);
        }
    };
}
