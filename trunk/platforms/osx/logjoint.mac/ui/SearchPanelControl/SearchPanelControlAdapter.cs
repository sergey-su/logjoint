using System;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SearchPanel;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class SearchPanelControlAdapter: NSViewController, IView
	{
		IViewEvents viewEvents;
		Dictionary<ViewCheckableControl, NSButton> checkableControls = new Dictionary<ViewCheckableControl, NSButton>();

		public SearchPanelControlAdapter()
		{
			NSBundle.LoadNib ("SearchPanelControl", this);

			checkableControls[ViewCheckableControl.MatchCase] = matchCaseCheckbox;
			checkableControls[ViewCheckableControl.WholeWord] = wholeWordCheckbox;
			checkableControls[ViewCheckableControl.RegExp] = regexCheckbox;
			checkableControls[ViewCheckableControl.SearchWithinThisThread] = searchInCurrentThreadCheckbox;
			checkableControls[ViewCheckableControl.SearchWithinCurrentLog] = searchInCurrentLogCheckbox;
			checkableControls[ViewCheckableControl.QuickSearch] = quickSearchRadioButton;
			checkableControls[ViewCheckableControl.SearchUp] = searchUpCheckbox;
			checkableControls[ViewCheckableControl.SearchInSearchResult] = searchInSearchResultsCheckbox;
			checkableControls[ViewCheckableControl.SearchAllOccurences] = searchAllRadioButton;
			checkableControls[ViewCheckableControl.SearchFromCurrentPosition] = fromCurrentPositionCheckbox;
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetSearchHistoryListEntries(object[] entries)
		{
			// todo
		}

		ViewCheckableControl IView.GetCheckableControlsState()
		{
			return checkableControls.Aggregate(0, 
				(i, ctrl) => ctrl.Value.State == NSCellStateValue.On ? i | (int)ctrl.Key : i,
				i => (ViewCheckableControl)i
			);
		}

		void IView.SetCheckableControlsState(ViewCheckableControl affectedControls, ViewCheckableControl checkedControls)
		{
			foreach (var ctrl in checkableControls)
				if ((ctrl.Key & affectedControls) != 0)
					ctrl.Value.State = (ctrl.Key & checkedControls) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;
		}

		void IView.EnableCheckableControls(ViewCheckableControl affectedControls, ViewCheckableControl enabledControls)
		{
			foreach (var ctrl in checkableControls)
				if ((ctrl.Key & affectedControls) != 0)
					ctrl.Value.Enabled = (ctrl.Key & enabledControls) != 0;
		}

		string IView.GetSearchTextBoxText()
		{
			return searchTextField.StringValue;
		}

		void IView.SetSearchTextBoxText(string value)
		{
			searchTextField.StringValue = value;
		}

		void IView.ShowErrorInSearchTemplateMessageBox()
		{
			// todo
		}

		void IView.FocusSearchTextBox()
		{
			searchTextField.Window.MakeFirstResponder(searchTextField);
		}

		partial void searchTextBoxEnterPressed (NSObject sender)
		{
			if (searchTextField.StringValue != "")
				viewEvents.OnSearchTextBoxEnterPressed();
		}

		partial void OnSearchModeChanged (NSObject sender)
		{
			viewEvents.OnSearchModeControlChecked(checkableControls.FirstOrDefault(ctrl => ctrl.Value == sender).Key);
		}
	}
}

