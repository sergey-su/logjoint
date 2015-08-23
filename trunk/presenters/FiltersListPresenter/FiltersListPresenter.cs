using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.FiltersListBox
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(IModel model, IFiltersList filtersList, IView view, FilterDialog.IPresenter filtersDialogPresenter)
		{
			this.model = model;
			this.filtersList = filtersList;
			this.view = view;
			this.filtersDialogPresenter = filtersDialogPresenter;
			this.isHighlightFilter = filtersList == model.HighlightFilters;
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
						lvi.Checked = f.Enabled;
						lvi.SetImageType(f.Action == FilterAction.Exclude ? ViewItemImageType.Exclude : ViewItemImageType.Include);
						lvi.SetSubText(GetFilterCounterString(filters.FilteringEnabled, f.Action, f.Counter, isHighlightFilter));
					}
					else
					{
						lvi.Text = "-";
						lvi.Checked = false;
						lvi.SetImageType(ViewItemImageType.None);
						lvi.SetSubText("");
					}
				}

				if (filters.Count == 0)
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
						defActionItem.Checked = true;
					}
					if (filters.GetDefaultAction() == FilterAction.Exclude)
					{
						if (isHighlightFilter)
							defActionItem.Text = "Don't hightlight by-default";
						else
							defActionItem.Text = "Hide all by-default";
						defActionItem.SetImageType(ViewItemImageType.Exclude);
					}
					else
					{
						if (isHighlightFilter)
							defActionItem.Text = "Highlight all by-default";
						else
							defActionItem.Text = "Show all by-default";
						defActionItem.SetImageType(ViewItemImageType.Include);
					}
					defActionItem.SetSubText(GetFilterCounterString(
						filters.FilteringEnabled, filters.GetDefaultAction(), filters.GetDefaultActionCounter(), isHighlightFilter));
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

		public static string GetFilterCounterString(bool filteringEnabled, FilterAction action, int counter, bool isHighlightFilter)
		{
			string fmt;
			if (!filteringEnabled)
			{
				if (isHighlightFilter)
				{
					fmt = "<highlighting disabled>";
				}
				else
				{
					fmt = "<filtering disabled>";
				}
			}
			else
			{
				if (isHighlightFilter)
				{
					fmt = action == FilterAction.Exclude ?
						"{0} message(s) excluded" : "{0} message(s) highlighted";
				}
				else
				{
					fmt = action == FilterAction.Exclude ?
						"{0} message(s) filtered out" : "{0} message(s) shown";
				}
			}
			return string.Format(fmt, counter);
		}

		void IViewEvents.OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		void IViewEvents.OnItemChecked(IViewItem item)
		{
			if (updateLock > 0)
				return;
			IFilter s = item.Filter;
			if (s != null && s.Enabled != item.Checked)
			{
				s.Enabled = item.Checked;
			}
			OnFilterChecked();
		}

		void IViewEvents.OnContextMenuOpening(out ContextMenuItem enabledItems, out ContextMenuItem checkedItems)
		{
			var f = GetTheOnly();

			enabledItems = ContextMenuItem.None;
			if (f != null)
				enabledItems |= (ContextMenuItem.FilterEnabled | ContextMenuItem.Properties);

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

		void IViewEvents.OnPropertiesMenuItemClicked()
		{
			IFilter f = GetTheOnly();
			if (f == null)
				return;
			filtersDialogPresenter.ShowTheDialog(f);
		}

		void IViewEvents.OnEnterPressed()
		{
			IFilter f = GetTheOnly();
			if (f == null)
				return;
			filtersDialogPresenter.ShowTheDialog(f);
		}

		void IViewEvents.OnDeletePressed()
		{
			if (DeleteRequested != null)
				DeleteRequested(this, EventArgs.Empty);
		}

		#region Implementation

		IEnumerable<IFilter> GetSelectedFilters()
		{
			return view.SelectedItems.Select(i => i.Filter).Where(f => f != null);
		}

		IFilter GetTheOnly()
		{
			var selectedItems = GetSelectedFilters().ToArray();
			if (selectedItems.Length != 1)
				return null;
			return selectedItems[0];
		}

		void OnFilterChecked()
		{
			if (FilterChecked != null)
				FilterChecked(this, EventArgs.Empty);
		}


		readonly IModel model;
		readonly IFiltersList filtersList;
		readonly bool isHighlightFilter;
		readonly IView view;
		readonly FilterDialog.IPresenter filtersDialogPresenter;
		int updateLock;

		#endregion
	};
};