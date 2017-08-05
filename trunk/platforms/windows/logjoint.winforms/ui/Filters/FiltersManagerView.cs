using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersManager;

namespace LogJoint.UI
{
	public partial class FiltersManagerView : UserControl, IView
	{
		IViewEvents presenter;
		Dictionary<ViewControl, Control> controls;

		public FiltersManagerView()
		{
			InitializeComponent();
			controls = new Dictionary<ViewControl, Control>()
			{
				{   ViewControl.AddFilterButton, addFilterButton     },
				{   ViewControl.RemoveFilterButton, deleteFilterButton      },
				{   ViewControl.MoveUpButton, moveFilterUpButton      },
				{   ViewControl.MoveDownButton, moveFilterDownButton        },
				{   ViewControl.NextButton, nextButton      },
				{   ViewControl.PrevButton, prevButton      },
				{   ViewControl.FilteringEnabledCheckbox, enableFilteringCheckBox     },
				{   ViewControl.FilterOptions, editButton     },
			};
		}

		Presenters.FiltersListBox.IView IView.FiltersListView { get { return this.filtersListView; } }

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.EnableControls(ViewControl controlsToEnable)
		{
			foreach (var c in controls)
				c.Value.Enabled = (controlsToEnable & c.Key) != 0;
		}

		void IView.SetControlsVisibility(ViewControl controlsToShow)
		{
			foreach (var c in controls)
				c.Value.Visible = (controlsToShow & c.Key) != 0;
		}

		void IView.SetFiltertingEnabledCheckBoxValue(bool value, string tooltip)
		{
			enableFilteringCheckBox.Checked = value;
			toolTip1.SetToolTip(enableFilteringCheckBox, tooltip ?? "");
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

		private void editButton_Click(object sender, EventArgs e)
		{
			presenter.OnOptionsClicked();
		}
	}
}
