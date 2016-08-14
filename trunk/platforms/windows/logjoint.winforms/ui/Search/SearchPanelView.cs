using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
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

			searchTextBox.Search += delegate(object sender, EventArgs args)
			{
				presenter.OnSearchTextBoxEnterPressed();
			};
			searchTextBox.Escape += delegate(object sender, EventArgs args)
			{
				presenter.OnSearchTextBoxEscapePressed();
			};
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.SetSearchHistoryListEntries(object[] entries)
		{
			searchTextBox.BeginUpdate();
			searchTextBox.Items.Clear();
			searchTextBox.Items.AddRange(entries);
			searchTextBox.EndUpdate();
		}

		void IView.ShowErrorInSearchTemplateMessageBox()
		{
			MessageBox.Show("Error in search template", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		void IView.FocusSearchTextBox()
		{
			searchTextBox.Focus();
			searchTextBox.SelectAll();
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

		string IView.GetSearchTextBoxText()
		{
			return searchTextBox.Text;
		}

		void IView.SetSearchTextBoxText(string value)
		{
			searchTextBox.Text = value;
		}


		IEnumerable<CheckableCtrl> EnumCheckableControls()
		{
			yield return new CheckableCtrl(ViewCheckableControl.MatchCase, matchCaseCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.WholeWord, wholeWordCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.RegExp, regExpCheckBox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchWithinThisThread, searchWithinCurrentThreadCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.Errors, searchMessageTypeCheckBox0);
			yield return new CheckableCtrl(ViewCheckableControl.Warnings, searchMessageTypeCheckBox1);
			yield return new CheckableCtrl(ViewCheckableControl.Infos, searchMessageTypeCheckBox2);
			yield return new CheckableCtrl(ViewCheckableControl.Frames, searchMessageTypeCheckBox3);
			yield return new CheckableCtrl(ViewCheckableControl.QuickSearch, searchNextMessageRadioButton);
			yield return new CheckableCtrl(ViewCheckableControl.SearchUp, searchUpCheckbox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchInSearchResult, searchInSearchResultsCheckBox);
			yield return new CheckableCtrl(ViewCheckableControl.SearchAllOccurences, searchAllOccurencesRadioButton);
			yield return new CheckableCtrl(ViewCheckableControl.SearchFromCurrentPosition, fromCurrentPositionCheckBox);
		}

		private void searchTextBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (searchTextBox.SelectedIndex >= 0 && searchTextBox.SelectedIndex < searchTextBox.Items.Count)
				presenter.OnSearchTextBoxSelectedEntryChanged(searchTextBox.Items[searchTextBox.SelectedIndex]);
		}

		private void searchTextBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();
			string textToDraw;
			presenter.OnSearchTextBoxEntryDrawing(searchTextBox.Items[e.Index], out textToDraw);
			if (textToDraw == null)
				return;
			using (var brush = new SolidBrush(e.ForeColor))
				e.Graphics.DrawString(textToDraw, e.Font, brush, e.Bounds);
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

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Enter)
			{
				presenter.OnSearchTextBoxEnterPressed();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
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
