using System;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.SearchPanel;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class SearchPanelControlAdapter: NSViewController, IView
	{
		IViewModel viewEvents;
		Dictionary<ViewCheckableControl, NSButton> checkableControls = new Dictionary<ViewCheckableControl, NSButton>();
		QuickSearchTextBoxAdapter quickSearchTextBox;

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

			quickSearchTextBox = new QuickSearchTextBoxAdapter ();
			quickSearchTextBox.View.MoveToPlaceholder(quickSearchPlaceholder);
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewEvents = viewModel;

			var updateControls = Updaters.Create (
				() => viewModel.CheckableControlsState,
				SetCheckableControlsState);
			var enableControls = Updaters.Create (
				() => viewModel.EnableCheckableControls,
				EnableCheckableControls);
			var updateFilter = Updaters.Create (
				() => viewModel.FiltersLink,
				value => {
					filtersLink.Hidden = !value.isVisible;
					filtersLink.StringValue = value.text;
				});

			viewModel.ChangeNotification.CreateSubscription (() => {
				updateControls ();
				enableControls ();
				updateFilter ();
			});
		}

		void SetCheckableControlsState(ViewCheckableControl checkedControls)
		{
			foreach (var ctrl in checkableControls)
				ctrl.Value.State = (ctrl.Key & checkedControls) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;
		}

		void EnableCheckableControls(ViewCheckableControl enabledControls)
		{
			foreach (var ctrl in checkableControls)
				ctrl.Value.Enabled = (ctrl.Key & enabledControls) != 0;
		}

		Presenters.QuickSearchTextBox.IView IView.SearchTextBox
		{
			get { return quickSearchTextBox; }
		}

		partial void OnSearchModeChanged (NSObject sender)
		{
			var c = checkableControls.FirstOrDefault (ctrl => ctrl.Value == sender);
			viewEvents.OnCheckControl(c.Key, c.Value.State == NSCellStateValue.On);
		}

		partial void OnFindClicked (NSObject sender)
		{
			viewEvents.OnSearchButtonClicked();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			filtersLink.LinkClicked = (s, e) => viewEvents.OnFiltersLinkClicked();
		}
	}
}