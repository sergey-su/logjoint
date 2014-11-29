using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.FiltersManager
{
	public interface IPresenter
	{
		FiltersListBox.IPresenter FiltersListPresenter { get; }
	};

	[Flags]
	public enum ViewControl
	{
		None = 0,
		AddFilterButton = 1,
		RemoveFilterButton = 2,
		MoveUpButton = 4,
		MoveDownButton = 8,
		PrevButton = 16,
		NextButton = 32,
		FilteringEnabledCheckbox = 64
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		void SetControlsVisibility(ViewControl controlsToShow);
		void EnableControls(ViewControl controlsToEnable);
		void SetFiltertingEnabledCheckBoxValue(bool value);
		void SetFiltertingEnabledCheckBoxLabel(string value);
		void ShowTooManyFiltersAlert(string text);
		bool AskUserConfirmationToDeleteFilters(int nrOfFiltersToDelete);
	};

	public interface IViewEvents
	{
		void OnEnableFilteringChecked(bool value);
		void OnAddFilterClicked();
		void OnRemoveFilterClicked();
		void OnMoveFilterUpClicked();
		void OnMoveFilterDownClicked();
		void OnPrevClicked();
		void OnNextClicked();
	};
};