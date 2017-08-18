using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchPanel;

namespace LogJoint.UI
{
	public partial class SearchPanelView : UserControl, IView
	{
		IViewEvents presenter;

		public SearchPanelView()
		{
			InitializeComponent();
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		Presenters.QuickSearchTextBox.IView IView.SearchTextBox => searchTextBox.InnerTextBox;

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

		ViewCheckableControl IView.GetCheckableControlsState()
		{
			return EnumCheckableControls().Aggregate(ViewCheckableControl.None, 
				(checkedCtrls, ctrl) => checkedCtrls | (ctrl.ControlChecked ? ctrl.ID : ViewCheckableControl.None));
		}

		void IView.SetCheckableControlsState(ViewCheckableControl affectedControls, ViewCheckableControl checkedControls)
		{
			foreach (var affectedCtrl in EnumCheckableControls().Where(ctrl => (affectedControls & ctrl.ID) != 0))
				affectedCtrl.ControlChecked = (checkedControls & affectedCtrl.ID) != 0;
		}

		void IView.EnableCheckableControls(ViewCheckableControl affectedControls, ViewCheckableControl enabledControls)
		{
			foreach (var affectedCtrl in EnumCheckableControls().Where(ctrl => (affectedControls & ctrl.ID) != 0))
				affectedCtrl.Control.Enabled = (affectedCtrl.ID & enabledControls) != 0;
		}

		void IView.SetSelectedSearchSuggestionLink(bool isVisible, string text)
		{
			currentSuggestionLinkLabel.Visible = isVisible;
			currentSuggestionLinkLabel.Text = text ?? "";
		}

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
			presenter.OnSearchButtonClicked();
		}

		private void searchModeRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			presenter.OnSearchModeControlChecked(
				sender == searchAllOccurencesRadioButton ? ViewCheckableControl.SearchAllOccurences : ViewCheckableControl.QuickSearch);
		}

		private void currentSuggestionLinkLabel_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			presenter.OnCurrentSuggestionLinkClicked();
		}
	}

	class SearchResultsPanelView : ISearchResultsPanelView
	{
		public SplitContainer container;

		bool ISearchResultsPanelView.Collapsed
		{
			get { return container.Panel2Collapsed; }
			set { container.Panel2Collapsed = value; }
		}
	};
}
