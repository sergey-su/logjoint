using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersManager;
using LogJoint.UI.Windows.Reactive;

namespace LogJoint.UI
{
    public partial class FiltersManagerView : UserControl
    {
        IViewModel viewModel;
        readonly Dictionary<ViewControl, Control> controls;
        ISubscription subscription;
        FilterDialog filterDialog;

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


        public void SetViewModel(IViewModel viewModel, IReactive reactive)
        {
            this.viewModel = viewModel;
            filtersListView.SetViewModel(viewModel.FiltersListBox);
            filterDialog = new FilterDialog(viewModel.FilterDialog, reactive);

            var enableControls = Updaters.Create(() => viewModel.EnabledControls, EnableControls);
            var setVisibility = Updaters.Create(() => viewModel.VisibileControls, SetControlsVisibility);
            var updateCheckbox = Updaters.Create(() => viewModel.FiltertingEnabledCheckBox, state =>
            {
                enableFilteringCheckBox.Checked = state.isChecked;
                toolTip1.SetToolTip(enableFilteringCheckBox, state.tooltip ?? "");
                enableFilteringCheckBox.Text = state.label;
            });
            subscription = viewModel.ChangeNotification.CreateSubscription(() =>
            {
                enableControls();
                setVisibility();
                updateCheckbox();
            });
        }

        void EnableControls(ViewControl controlsToEnable)
        {
            foreach (var c in controls)
                c.Value.Enabled = (controlsToEnable & c.Key) != 0;
        }

        void SetControlsVisibility(ViewControl controlsToShow)
        {
            foreach (var c in controls)
                c.Value.Visible = (controlsToShow & c.Key) != 0;
        }

        private void addFilterButton_Click(object sender, EventArgs e)
        {
            viewModel.OnAddFilterClicked();
        }

        private void deleteFilterButton_Click(object sender, EventArgs e)
        {
            viewModel.OnRemoveFilterClicked();
        }

        private void moveFilterUpButton_Click(object sender, EventArgs e)
        {
            viewModel.OnMoveFilterUpClicked();
        }

        private void moveFilterDownButton_Click(object sender, EventArgs e)
        {
            viewModel.OnMoveFilterDownClicked();
        }

        private void enableFilteringCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            viewModel.OnEnableFilteringChecked(enableFilteringCheckBox.Checked);
        }

        private void prevButton_Click(object sender, EventArgs e)
        {
            viewModel.OnPrevClicked();
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            viewModel.OnNextClicked();
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            viewModel.OnOptionsClicked();
        }
    }
}
