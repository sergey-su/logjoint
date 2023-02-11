using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LogJoint.UI.Presenters.FiltersListBox
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly IChangeNotification changeNotification;
		IFiltersList filtersList;
		readonly FilterDialog.IPresenter filtersDialogPresenter;
		Func<IReadOnlyList<IViewItem>> items;
		IImmutableSet<IFilter> selectedFiltersWithNull = ImmutableHashSet<IFilter>.Empty;
		Func<IImmutableSet<IFilter>> selectedFilters;

		public Presenter(
			IChangeNotification changeNotification,
			FilterDialog.IPresenter filtersDialogPresenter,
			IColorTable highlightColorsTable
		)
		{
			this.changeNotification = changeNotification;
			this.filtersDialogPresenter = filtersDialogPresenter;
			this.items = Selectors.Create(
				() => filtersList?.Items, () => filtersList?.GetDefaultAction(),
				() => filtersList?.Purpose, () => highlightColorsTable.Items,
				() => selectedFiltersWithNull,
				GetViewItems);
			this.selectedFilters = Selectors.Create(() => selectedFiltersWithNull, s => s.Remove(null));
		}

		public event EventHandler DeleteRequested;

		IFiltersList IPresenter.FiltersList 
		{
			get { return filtersList; } 
			set
			{ 
				filtersList = value;
				changeNotification.Post();
			} 
		}
		bool IViewModel.IsEnabled => filtersList != null && filtersList.FilteringEnabled;


		IImmutableSet<IFilter> IPresenter.SelectedFilters => selectedFilters();

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IReadOnlyList<IViewItem> IViewModel.Items => items();

		void IViewModel.OnChangeSelection(IViewItem[] items)
		{
			selectedFiltersWithNull = ImmutableHashSet.CreateRange(items.Select(i => (i as ViewItem)?.Filter));
			changeNotification.Post();
		}

		void IViewModel.OnItemChecked(IViewItem item)
		{
			IFilter s = (item as ViewItem)?.Filter;
			if (s != null && !s.IsDisposed && s.Enabled != item.IsChecked)
			{
				s.Enabled = item.IsChecked.GetValueOrDefault();
			}
		}

		void IViewModel.OnContextMenuOpening(out ContextMenuItem enabledItems, out ContextMenuItem checkedItems)
		{
			var f = GetTheOnly();

			enabledItems = ContextMenuItem.None;
			if (f != null)
			{
				enabledItems |= (ContextMenuItem.FilterEnabled | ContextMenuItem.Properties);
				if (f != filtersList.Items.FirstOrDefault())
					enabledItems |= ContextMenuItem.MoveUp;
				if (f != filtersList.Items.LastOrDefault())
					enabledItems |= ContextMenuItem.MoveDown;
			}

			checkedItems = ContextMenuItem.None;
			if (f == null || f.Enabled)
				checkedItems |= ContextMenuItem.FilterEnabled;
		}

		void IViewModel.OnFilterEnabledMenuItemClicked()
		{
			IFilter f = GetTheOnly();
			if (f != null)
			{
				f.Enabled = !f.Enabled;
			}
		}

		void IViewModel.OnDoubleClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f, filtersList.Purpose);
		}

		void IViewModel.OnPropertiesMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f, filtersList.Purpose);
		}

		void IViewModel.OnMoveUpMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersList.Move(f, upward: true);
		}

		void IViewModel.OnMoveDownMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersList.Move(f, upward: false);
		}

		void IViewModel.OnEnterPressed()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f, filtersList.Purpose);
		}

		void IViewModel.OnDeletePressed()
		{
			DeleteRequested?.Invoke (this, EventArgs.Empty);
		}

		IFilter GetTheOnly()
		{
			if (selectedFiltersWithNull.Count != 1)
				return null;
			var f = selectedFiltersWithNull.Single();
			if (f == null || f.IsDisposed)
				return null;
			return f;
		}

		static IReadOnlyList<IViewItem> GetViewItems(
			IReadOnlyList<IFilter> filters, FilterAction? defaultAction, FiltersListPurpose? purpose,
			ImmutableArray<Color> highlightColorsTable, IImmutableSet<IFilter> selectedFilters)
		{
			var builder = ImmutableList.CreateBuilder<IViewItem>();
			if (filters != null)
			{
				foreach (var f in filters)
				{
					builder.Add(new ViewItem(f, highlightColorsTable, purpose.Value, selectedFilters.Contains(f)));
				}
				if (filters.Count > 0)
				{
					builder.Add(new DefaultActionViewItem(defaultAction.Value, purpose.Value, selectedFilters.Contains(null)));
				}
			}
			return builder.ToImmutable();
		}

		class DefaultActionViewItem : IViewItem
		{
			readonly string text;
			readonly ViewItemImageType imageType;
			readonly bool selected;

			public DefaultActionViewItem(FilterAction defaultAction, FiltersListPurpose purpose, bool selected)
			{
				this.selected = selected;
				if (defaultAction == FilterAction.Exclude)
				{
					if (purpose == FiltersListPurpose.Highlighting)
						text = "Don't highlight by-default";
					else if (purpose == FiltersListPurpose.Search)
						text = "Exclude from search results by-default";
					else
						text = "Exclude all by-default";
					imageType = ViewItemImageType.Exclude;
				}
				else
				{
					if (purpose == FiltersListPurpose.Highlighting)
						text = "Highlight all by-default";
					else if (purpose == FiltersListPurpose.Search)
						text = "Include all to search results by-default";
					else
						text = "Include all by-default";
					imageType = ViewItemImageType.Include;
				}
			}

			public override string ToString() => text;

			Color? IViewItem.Color => null;

			bool? IViewItem.IsChecked => null;

			string IViewItem.CheckboxTooltip => "";

			string IViewItem.ActionTooltip => "";

			ViewItemImageType IViewItem.ImageType => imageType;

			bool IViewItem.IsDefaultActionItem => true;

			string IListItem.Key => "def";

			bool IListItem.IsSelected => selected;
		}

		class ViewItem : IViewItem
		{
			readonly IFilter filter;
			readonly IReadOnlyList<Color> highlightColorsTable;
			readonly string actionTooltip;
			readonly bool selected;

			public ViewItem(IFilter filter, IReadOnlyList<Color> highlightColorsTable, FiltersListPurpose purpose, bool selected)
			{
				this.filter = filter;
				this.highlightColorsTable = highlightColorsTable;
				this.selected = selected;
				if (purpose == FiltersListPurpose.Highlighting)
				{
					if (filter.Action == FilterAction.Exclude)
						actionTooltip = "Rule excludes matched messages from highlighting";
					else
						actionTooltip = string.Format(
							"Rule highlights matched messages with color #{0}",
							filter.Action - FilterAction.IncludeAndColorizeFirst + 1);
				}
				else if (purpose == FiltersListPurpose.Search)
				{
					if (filter.Action == FilterAction.Exclude)
						actionTooltip = "Rule excludes matched messages from search results";
					else
						actionTooltip = string.Format(
							"Rule includes matched messages to search results" +
							(filter.Action == FilterAction.Include
							 ? "" : " and highlights them with color #{0}"),
								filter.Action - FilterAction.IncludeAndColorizeFirst + 1);
				}
			}

			public override string ToString() => filter.IsDisposed ? "-" : filter.Name;

			public IFilter Filter => filter;

			bool? IViewItem.IsChecked => filter.IsDisposed ? new bool?() : filter.Enabled;

			Color? IViewItem.Color => filter.IsDisposed ? new Color?() : filter.Action.ToColor(highlightColorsTable);

			string IViewItem.CheckboxTooltip =>
				filter.IsDisposed ? "" :
				filter.Enabled ? "Uncheck to disable rule without deleting it" :
					"Check to enable rule";

			string IViewItem.ActionTooltip => filter.IsDisposed ? "" : actionTooltip;

			ViewItemImageType IViewItem.ImageType => 
				filter.IsDisposed ? ViewItemImageType.None :
				filter.Action == FilterAction.Exclude ? ViewItemImageType.Exclude : 
					ViewItemImageType.Include;

			bool IViewItem.IsDefaultActionItem => false;

			string IListItem.Key => filter.GetHashCode().ToString();

			bool IListItem.IsSelected => selected;
		}
	};
};