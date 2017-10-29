using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesManager;

namespace LogJoint.UI
{
	public partial class SourcesManagementView : UserControl, IView
	{
		public SourcesManagementView()
		{ 
			InitializeComponent();
		}

		public SourcesListView SourcesListView
		{
			get { return sourcesListView; }
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			mruContextMenuStrip.Items.Clear();
			foreach (var item in items)
			{
				mruContextMenuStrip.Items.Add(new ToolStripMenuItem(item.Text)
				{
					Tag = item.Data,
					Enabled = !item.Disabled,
					ShortcutKeyDisplayString = item.InplaceAnnotation ?? "",
					ToolTipText = item.ToolTip ?? ""
				});
			}
			mruContextMenuStrip.Show(recentButton, new Point(0, recentButton.Height));
		}

		void IView.EnableDeleteAllSourcesButton(bool enable)
		{
			deleteAllButton.Enabled = enable;
		}

		void IView.EnableDeleteSelectedSourcesButton(bool enable)
		{
			deleteButton.Enabled = enable;
		}

		void IView.EnableTrackChangesCheckBox(bool enable)
		{
			trackChangesCheckBox.Enabled = enable;
		}

		void IView.SetTrackingChangesCheckBoxState(TrackingChangesCheckBoxState state)
		{
			CheckState newState;
			if (state == TrackingChangesCheckBoxState.Indeterminate)
				newState = CheckState.Indeterminate;
			else if (state == TrackingChangesCheckBoxState.Checked)
				newState = CheckState.Checked;
			else
				newState = CheckState.Unchecked;

			if (trackChangesCheckBox.CheckState != newState)
				trackChangesCheckBox.CheckState = newState;
		}

		void IView.SetShareButtonState(bool visible, bool enabled, bool progress)
		{
			shareButton.Visible = visible;
			shareButton.Enabled = enabled;
			if (progress)
			{
				shareButton.Image = LogJoint.Properties.Resources.loader;
				shareButton.ImageAlign = ContentAlignment.MiddleLeft;
			}
			else
			{
				shareButton.Image = null;
			}
		}

		void IView.SetPropertiesButtonState(bool enabled)
		{
			propertiesButton.Enabled = enabled;
		}

		string IView.ShowOpenSingleFileDialog()
		{
			return null;
		}

		private void addNewLogButton_Click(object sender, EventArgs e)
		{
			presenter.OnAddNewLogButtonClicked();
		}

		private void shareButton_Click(object sender, EventArgs e)
		{
			presenter.OnShareButtonClicked();
		}

		private void propertiesButton_Click(object sender, EventArgs e)
		{
			presenter.OnPropertiesButtonClicked();
		}

		private void deleteButton_Click(object sender, EventArgs e)
		{
			presenter.OnDeleteSelectedLogSourcesButtonClicked();
		}

		private void deleteAllButton_Click(object sender, EventArgs e)
		{
			presenter.OnDeleteAllLogSourcesButtonClicked();
		}

		private void recentButton_Click(object sender, EventArgs e)
		{
			presenter.OnShowHistoryDialogButtonClicked();
		}

		private void mruContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			// Hide the menu here to simplify debugging. Without Hide() the menu remains 
			// on the screen if execution stops at a breakepoint.
			mruContextMenuStrip.Hide();

			presenter.OnMRUMenuItemClicked(e.ClickedItem.Tag);
		}

		private void trackChangesCheckBox_Click(object sender, EventArgs e)
		{
			bool value = trackChangesCheckBox.CheckState == CheckState.Unchecked;
			presenter.OnTrackingChangesCheckBoxChecked(value);
		}

		IViewEvents presenter;

	}
}
