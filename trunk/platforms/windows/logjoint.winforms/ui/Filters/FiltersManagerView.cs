using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersManager;

namespace LogJoint.UI
{
	public partial class FiltersManagerView : UserControl, IView
	{
		IViewEvents presenter;

		public FiltersManagerView()
		{
			InitializeComponent();
		}

		public FiltersListView FiltersListView { get { return this.filtersListView; } }

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.EnableControls(ViewControl controlsToEnable)
		{
			addFilterButton.Enabled = (controlsToEnable & ViewControl.AddFilterButton) != 0;
			deleteFilterButton.Enabled = (controlsToEnable & ViewControl.RemoveFilterButton) != 0;
			moveFilterUpButton.Enabled = (controlsToEnable & ViewControl.MoveUpButton) != 0;
			moveFilterDownButton.Enabled = (controlsToEnable & ViewControl.MoveDownButton) != 0;
			nextButton.Enabled = (controlsToEnable & ViewControl.NextButton) != 0;
			prevButton.Enabled = (controlsToEnable & ViewControl.PrevButton) != 0;
			enableFilteringCheckBox.Enabled = (controlsToEnable & ViewControl.FilteringEnabledCheckbox) != 0;
		}

		void IView.SetControlsVisibility(ViewControl controlsToShow)
		{
			nextButton.Visible = (controlsToShow & ViewControl.NextButton) != 0;
			prevButton.Visible = (controlsToShow & ViewControl.PrevButton) != 0;
		}

		bool IView.AskUserConfirmationToDeleteFilters(int nrOfFiltersToDelete)
		{
			return MessageBox.Show(
				string.Format("You are about to delete ({0}) filter(s).\nAre you sure?", nrOfFiltersToDelete),
					"LogJoint", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes;
		}


		void IView.SetFiltertingEnabledCheckBoxValue(bool value)
		{
			enableFilteringCheckBox.Checked = value;
		}

		void IView.SetFiltertingEnabledCheckBoxLabel(string value)
		{
			enableFilteringCheckBox.Text = value;
		}

		private void addFilterButton_Click(object sender, EventArgs e)
		{
			presenter.OnAddFilterClicked();
		}

		private void deleteFilterButton_Click(object sender, EventArgs e)
		{
			presenter.OnRemoveFilterClicked();
		}

		private void moveFilterUpButton_Click(object sender, EventArgs e)
		{
			presenter.OnMoveFilterUpClicked();
		}

		private void moveFilterDownButton_Click(object sender, EventArgs e)
		{
			presenter.OnMoveFilterDownClicked();
		}

		private void enableFilteringCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			presenter.OnEnableFilteringChecked(enableFilteringCheckBox.Checked);
		}

		private void prevButton_Click(object sender, EventArgs e)
		{
			presenter.OnPrevClicked();
		}

		private void nextButton_Click(object sender, EventArgs e)
		{
			presenter.OnNextClicked();
		}
	}
}
