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

		public OptionsDialog(Windows.Reactive.IReactive reactive)
		{
			InitializeComponent();
			pluginsView1.Init(reactive);
			pages = new Dictionary<PageId, TabPage>
			{
				{ PageId.Plugins, pluginsTabPage },
				{ PageId.UpdatesAndFeedback, updatesAndFeedbackTabPage },
				{ PageId.Appearance, appearanceTabPage },
				{ PageId.MemAndPerformance, memAndPerformanceTabPage },
			};
		}

		void IDialog.SetViewModel(IDialogViewModel value)
		{
			this.viewModel = value;
			foreach (var p in pages)
				SetPageVisibility((viewModel.VisiblePages & p.Key) != 0, p.Value);
		}

		void IDialog.Show(PageId? initiallySelectedPage)
		{
			if (initiallySelectedPage.HasValue && pages.TryGetValue(initiallySelectedPage.Value, out var p))
				tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(p);
			this.ShowDialog();
		}

		void IDialog.Hide()
		{
			DialogResult = DialogResult.OK;
		}

		Presenters.Options.MemAndPerformancePage.IView IDialog.MemAndPerformancePage
		{
			get { return memAndPerformanceSettingsView; }
		}

		Presenters.Options.Appearance.IView IDialog.ApperancePage
		{
			get { return appearanceSettingsView1; }
		}

		Presenters.Options.UpdatesAndFeedback.IView IDialog.UpdatesAndFeedbackPage
		{
			get { return updatesAndFeedbackView1; }
		}

		Presenters.Options.Plugins.IView IDialog.PluginsPage => pluginsView1;

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

		IDialog IView.CreateDialog()
		{
			return new OptionsDialog(reactive);
		}
	};
}
