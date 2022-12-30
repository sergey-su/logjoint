using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.FiltersManager
{
	public interface IPresenter: IDisposable
	{
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
		FilteringEnabledCheckbox = 64,
		FilterOptions = 128,
	};

	public interface IView
	{
		void SetPresenter(IViewModel presenter);
		FiltersListBox.IView FiltersListView { get; }
		void SetControlsVisibility(ViewControl controlsToShow);
		void EnableControls(ViewControl controlsToEnable);
		void SetFiltertingEnabledCheckBoxValue(bool value, string tooltip);
		void SetFiltertingEnabledCheckBoxLabel(string value);
	};

	public interface IViewModel
	{
		void OnEnableFilteringChecked(bool value);
		void OnAddFilterClicked();
		void OnRemoveFilterClicked();
		void OnMoveFilterUpClicked();
		void OnMoveFilterDownClicked();
		void OnPrevClicked();
		void OnNextClicked();
		void OnOptionsClicked();
	};
};