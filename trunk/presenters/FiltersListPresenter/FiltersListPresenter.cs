using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.FiltersListBox
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IFiltersList filtersList, 
			IView view, 
			FilterDialog.IPresenter filtersDialogPresenter
		)
		{
			this.filtersList = filtersList;
			this.view = view;
			this.filtersDialogPresenter = filtersDialogPresenter;
			view.SetPresenter(this);
		}

		public event EventHandler FilterChecked;
		public event EventHandler SelectionChanged;
		public event EventHandler DeleteRequested;

		IFiltersList IPresenter.FiltersList { get { return filtersList; } }

		void IPresenter.SelectFilter(IFilter filter)
		{
			for (int i = 0; i < view.Count; ++i)
				view.GetItem(i).Selected = view.GetItem(i).Filter == filter;
		}

		void IPresenter.UpdateView()
		{
			updateLock++;	
			view.BeginUpdate();
			try
			{
				IFiltersList filters = filtersList;
				IViewItem defActionItem = null;
				for (int i = view.Count - 1; i >= 0; --i)
				{
					IFilter ls = view.GetItem(i).Filter;
					if (ls == null)
						defActionItem = view.GetItem(i);
					else if (ls.Owner == null)
						view.RemoveAt(i);
				}
				int filterIdx = 0;
				foreach (var f in filters.Items)
				{
					IViewItem lvi;
					int existingItemIdx = view.GetItemIndexByKey(f.GetHashCode().ToString());
					if (existingItemIdx < 0)
					{
						lvi = view.CreateItem(f, f.GetHashCode().ToString());
						view.Insert(filterIdx, lvi);
					}
					else
					{
						lvi = view.GetItem(existingItemIdx);
						if (existingItemIdx != filterIdx)
						{
							view.RemoveAt(existingItemIdx);
							view.Insert(filterIdx, lvi);
						}
					}
					++filterIdx;

					if (!f.IsDisposed)
					{
						lvi.Text = f.Name;
						lvi.Color = f.Action.GetBackgroundColor();
						lvi.Checked = f.Enabled;
						if (f.Enabled)
							lvi.CheckboxTooltip = "Uncheck to disable rule without deleting it";
						else
							lvi.CheckboxTooltip = "Check to enable rule";
						lvi.SetImageType(f.Action == FilterAction.Exclude ? ViewItemImageType.Exclude : ViewItemImageType.Include);
						if (filtersList.Purpose == FiltersListPurpose.Highlighting)
						{
							if (f.Action == FilterAction.Exclude)
								lvi.ActionTooltip = "Rule excludes matched messages from highlighting";
							else
								lvi.ActionTooltip = string.Format(
									"Rule highlights matched messages with color #{0}", 
								    f.Action - FilterAction.IncludeAndColorizeFirst + 1);
						}
						else if (filtersList.Purpose == FiltersListPurpose.Search)
						{
							if (f.Action == FilterAction.Exclude)
								lvi.ActionTooltip = "Rule excludes matched messages from search results";
							else
								lvi.ActionTooltip = string.Format(
									"Rule includes matched messages to search results" + 
									(f.Action == FilterAction.Include
									 ? "" : " and highlights them with color #{0}"), 
										f.Action - FilterAction.IncludeAndColorizeFirst + 1);
						}
					}
					else
					{
						lvi.Text = "-";
						lvi.Checked = null;
						lvi.CheckboxTooltip = "";
						lvi.Color = null;
						lvi.SetImageType(ViewItemImageType.None);
					}
				}

				if (filters.Items.Count == 0)
				{
					if (defActionItem != null)
					{
						view.Remove(defActionItem);
					}
				}
				else
				{
					if (defActionItem == null)
					{
						defActionItem = view.CreateItem(null, "");
						view.Insert(filterIdx, defActionItem);
						defActionItem.Checked = null;
					}
					if (filters.GetDefaultAction() == FilterAction.Exclude)
					{
						if (filtersList.Purpose == FiltersListPurpose.Highlighting)
							defActionItem.Text = "Don't hightlight by-default";
						else if (filtersList.Purpose == FiltersListPurpose.Search)
							defActionItem.Text = "Exclude from search results by-default";
						else 
							defActionItem.Text = "Exclude all by-default";
						defActionItem.SetImageType(ViewItemImageType.Exclude);
					}
					else
					{
						if (filtersList.Purpose == FiltersListPurpose.Highlighting)
							defActionItem.Text = "Highlight all by-default";
						else if (filtersList.Purpose == FiltersListPurpose.Search)
							defActionItem.Text = "Include all to search results by-default";
						else
							defActionItem.Text = "Include all by-default";
						defActionItem.SetImageType(ViewItemImageType.Include);
					}
				}

				view.SetEnabled(filters.FilteringEnabled);
			}
			finally
			{
				updateLock--;
				view.EndUpdate();
			}
		}

		IEnumerable<IFilter> IPresenter.SelectedFilters
		{
			get { return GetSelectedFilters(); }
		}

		void IViewEvents.OnSelectionChanged()
		{
			SelectionChanged?.Invoke (this, EventArgs.Empty);
		}

		void IViewEvents.OnItemChecked(IViewItem item)
		{
			if (updateLock > 0)
				return;
			IFilter s = item.Filter;
			if (s != null && !s.IsDisposed && s.Enabled != item.Checked)
			{
				s.Enabled = item.Checked.GetValueOrDefault();
				OnFilterChecked();
			}
		}

		void IViewEvents.OnContextMenuOpening(out ContextMenuItem enabledItems, out ContextMenuItem checkedItems)
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

		void IViewEvents.OnFilterEnabledMenuItemClicked()
		{
			IFilter f = GetTheOnly();
			if (f != null)
			{
				f.Enabled = !f.Enabled;
				OnFilterChecked();
			}
		}

		void IViewEvents.OnDoubleClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f);			
		}

		void IViewEvents.OnPropertiesMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f);
		}

		void IViewEvents.OnMoveUpMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersList.Move(f, upward: true);
		}

		void IViewEvents.OnMoveDownMenuItemClicked()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersList.Move(f, upward: false);
		}

		void IViewEvents.OnEnterPressed()
		{
			var f = GetTheOnly();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f);
		}

		void IViewEvents.OnDeletePressed()
		{
			DeleteRequested?.Invoke (this, EventArgs.Empty);
		}

		#region Implementation

		IEnumerable<IFilter> GetSelectedFilters()
		{
			return view.SelectedItems.Select(i => i.Filter).Where(f => f != null && !f.IsDisposed);
		}

		IFilter GetTheOnly()
		{
			var selectedItems = GetSelectedFilters().ToArray();
			if (selectedItems.Length != 1)
				return null;
			if (selectedItems[0].IsDisposed)
				return null;
			return selectedItems[0];
		}

		void OnFilterChecked()
		{
			FilterChecked?.Invoke(this, EventArgs.Empty);
		}

		readonly IFiltersList filtersList;
		readonly IView view;
		readonly FilterDialog.IPresenter filtersDialogPresenter;
		int updateLock;

		#endregion
	};
};