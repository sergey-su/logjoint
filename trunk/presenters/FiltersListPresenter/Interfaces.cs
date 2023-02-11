using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.FiltersListBox
{
	public interface IPresenter
	{
		IFiltersList FiltersList { get; set; }

		event EventHandler DeleteRequested;
		IImmutableSet<IFilter> SelectedFilters { get; }
	};

	public enum ViewItemImageType
	{
		None,
		Include,
		Exclude
	};

	public interface IViewItem : IListItem
	{
		Color? Color { get; }
		bool? IsChecked { get; }
		string CheckboxTooltip { get; }
		string ActionTooltip { get; }
		ViewItemImageType ImageType { get; }
		bool IsDefaultActionItem { get; }
	};

	[Flags]
	public enum ContextMenuItem
	{
		None = 0,
		FilterEnabled = 1,
		Properties = 2,
		MoveUp = 4,
		MoveDown = 8,
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		bool IsEnabled { get; }
		IReadOnlyList<IViewItem> Items { get; }
		void OnChangeSelection(IViewItem[] items);
		void OnItemChecked(IViewItem item);
		void OnContextMenuOpening(out ContextMenuItem enabledItems, out ContextMenuItem checkedItems);
		void OnFilterEnabledMenuItemClicked();
		void OnMoveUpMenuItemClicked();
		void OnMoveDownMenuItemClicked();
		void OnPropertiesMenuItemClicked();
		void OnEnterPressed();
		void OnDeletePressed();
		void OnDoubleClicked();
	};
};