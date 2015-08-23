using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.FiltersListBox
{
	public interface IPresenter
	{
		IFiltersList FiltersList { get; }
		event EventHandler FilterChecked;
		event EventHandler SelectionChanged;
		event EventHandler DeleteRequested;
		void SelectFilter(IFilter filter);
		IEnumerable<IFilter> SelectedFilters { get; }
		void UpdateView();
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		void BeginUpdate();
		void EndUpdate();

		IViewItem CreateItem(IFilter filter, string key);

		int Count { get; }
		IViewItem GetItem(int index);

		void RemoveAt(int index);
		void Remove(IViewItem item);
		void Insert(int index, IViewItem item);
		int GetItemIndexByKey(string key);

		IEnumerable<IViewItem> SelectedItems { get; }

		void SetEnabled(bool value);
	};

	public enum ViewItemImageType
	{
		None,
		Include,
		Exclude
	};

	public interface IViewItem
	{
		IFilter Filter { get; }
		string Text { get; set; }
		bool Checked { get; set; }
		bool Selected { get; set; }
		void SetImageType(ViewItemImageType imageType);
		void SetSubText(string text);
	};

	[Flags]
	public enum ContextMenuItem
	{
		None = 0,
		FilterEnabled = 1,
		Properties = 2,
	};

	public interface IViewEvents
	{
		void OnSelectionChanged();
		void OnItemChecked(IViewItem item);
		void OnContextMenuOpening(out ContextMenuItem enabledItems, out ContextMenuItem checkedItems);
		void OnFilterEnabledMenuItemClicked();
		void OnPropertiesMenuItemClicked();
		void OnEnterPressed();
		void OnDeletePressed();
	};
};