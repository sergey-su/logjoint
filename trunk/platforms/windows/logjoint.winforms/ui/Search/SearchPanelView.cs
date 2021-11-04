using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchPanel;

namespace LogJoint.UI
{
	public partial class SearchPanelView : UserControl
	{
		IViewModel viewModel;
		ISubscription subscription;

		public SearchPanelView()
		{
			InitializeComponent();
		}

		public void SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateChecked = Updaters.Create(() => viewModel.CheckableControlsState, value => 
			{
				foreach (var ctrl in EnumCheckableControls())
					ctrl.ControlChecked = (value & ctrl.ID) != 0;
			});
			var updateEnabled = Updaters.Create(() => viewModel.EnableCheckableControls, value =>
			{
				foreach (var ctrl in EnumCheckableControls())
					ctrl.Control.Enabled = (value & ctrl.ID) != 0;
			});
			var updateFilterLink = Updaters.Create(() => viewModel.FiltersLink, link =>
			{
				currentSuggestionLinkLabel.Visible = link.isVisible;
				currentSuggestionLinkLabel.Text = link.text ?? "";
			});

			this.subscription = viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateChecked();
				updateEnabled();
				updateFilterLink();
			});
		}

		class CheckableCtrl
		{
			public ViewCheckableControl ID;
			public ButtonBase Control;
			public bool ControlChecked
			{ 
				get
				{
					var cb = Control as CheckBox;
					if (cb != null)
						return cb.Checked;
					var rb = Control as RadioButton;
					if (rb != null)
						return rb.Checked;
					return false; 
				} 
				set 
				{
					var cb = Control as CheckBox;
					if (cb != null)
						cb.Checked = value;
					var rb = Control as RadioButton;
					if (rb != null)
						rb.Checked = value;
				}
			}
			public CheckableCtrl(ViewCheckableControl id, ButtonBase ctrl) { ID = id; Control = ctrl; }
		};

		IEnumerable<CheckableCtrl> EnumCheckableControls()
		{
			yield return new CheckableCtrl(ViewCheckableControl.MatchCase, matchCaseCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.WholeWord, wholeWordCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.RegExp, regExpCheckBox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchWithinThisThread, searchWithinCurrentThreadCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchWithinCurrentLog, searchWithinCurrentLogCheckBox);
			yield return new CheckableCtrl(ViewCheckableControl.QuickSearch, searchNextMessageRadioButton);
			yield return new CheckableCtrl(ViewCheckableControl.SearchUp, searchUpCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchInSearchResult, searchInSearchResultsCheckBox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchAllOccurences, searchAllOccurencesRadioButton);
			yield return new CheckableCtrl(ViewCheckableControl.SearchFromCurrentPosition, fromCurrentPositionCheckBox);
		}

		private void doSearchButton_Click(object sender, EventArgs e)
		{
			viewModel.OnSearchButtonClicked();
		}

		private void checkableControlCheckedChanged(object sender, EventArgs e)
		{
			var ctrl = EnumCheckableControls().FirstOrDefault(c => c.Control == sender);
			if (ctrl != null)
				viewModel.OnCheckControl(ctrl.ID, ctrl.ControlChecked);
		}

		private void currentSuggestionLinkLabel_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			viewModel.OnFiltersLinkClicked();
		}
	}
}
