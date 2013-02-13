using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class FiltersListView : UserControl
	{
		IFiltersListViewHost host;
		int updateLock;

		public FiltersListView()
		{
			InitializeComponent();
			InitCounterHeader();
		}

		void InitCounterHeader()
		{
			using (Graphics g = this.CreateGraphics())
			{
				int maxCounter = 999999;
				int maxWidth = 0;
				for (FilterAction a = FilterAction.Include; a <= FilterAction.Exclude; ++a)
					maxWidth = Math.Max(maxWidth, (int)g.MeasureString(GetFilterCounterString(true, a, maxCounter, true), this.Font).Width);
				counterColumnHeader.Width = maxWidth + 5;
			}
		}

		public void SetHost(IFiltersListViewHost host)
		{
			this.host = host;
		}

		Filter Get(int i)
		{
			return Get(list.Items[i]);
		}

		Filter Get(ListViewItem i)
		{
			return i.Tag as Filter;
		}

		public event EventHandler FilterChecked;
		public event EventHandler SelectionChanged;

		void OnFilterChecked()
		{
			if (FilterChecked != null)
				FilterChecked(this, EventArgs.Empty);
		}

		static string GetFilterCounterString(bool filteringEnabled, FilterAction action, int counter, bool isHighlightFilter)
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

		public void UpdateView()
		{
			updateLock++;
			list.BeginUpdate();
			try
			{
				FiltersList filters = host.Filters;
				ListViewItem defActionItem = null;
				for (int i = list.Items.Count - 1; i >= 0; --i)
				{
					Filter ls = Get(i);
					if (ls == null)
						defActionItem = list.Items[i];
					else if (ls.Owner == null)
						list.Items.RemoveAt(i);
				}
				int filterIdx = 0;
				foreach (Filter f in filters.Items)
				{
					ListViewItem lvi;
					int existingItemIdx = list.Items.IndexOfKey(f.GetHashCode().ToString());
					if (existingItemIdx < 0)
					{
						lvi = new ListViewItem();
						lvi.Tag = f;
						lvi.Name = f.GetHashCode().ToString();
						lvi.SubItems.Add("");
						list.Items.Insert(filterIdx, lvi);
					}
					else
					{
						lvi = list.Items[existingItemIdx];
						if (existingItemIdx != filterIdx)
						{
							list.Items.RemoveAt(existingItemIdx);
							list.Items.Insert(filterIdx, lvi);
						}
					}
					++filterIdx;

					if (!f.IsDisposed)
					{
						lvi.Text = f.Name;
						lvi.Checked = f.Enabled;
						lvi.ImageIndex = f.Action == FilterAction.Exclude ? 0 : 2;
						lvi.SubItems[1].Text = GetFilterCounterString(filters.FilteringEnabled, f.Action, f.Counter, host.IsHighlightFilter);
					}
					else
					{
						lvi.Text = "-";
						lvi.Checked = false;
						lvi.ImageIndex = 0;
						lvi.SubItems[1].Text = "";
					}
				}

				if (filters.Count == 0)
				{
					if (defActionItem != null)
					{
						list.Items.Remove(defActionItem);
					}
				}
				else
				{
					if (defActionItem == null)
					{
						defActionItem = new ListViewItem();
						list.Items.Insert(filterIdx, defActionItem);
						defActionItem.Checked = true;
						defActionItem.SubItems.Add("");
						defActionItem.ForeColor = SystemColors.GrayText;
					}
					if (filters.GetDefaultAction() == FilterAction.Exclude)
					{
						if (host.IsHighlightFilter)
							defActionItem.Text = "Don't hightlight by-default";
						else
							defActionItem.Text = "Hide all by-default";
						defActionItem.ImageIndex = 0;
					}
					else
					{
						if (host.IsHighlightFilter)
							defActionItem.Text = "Highlight all by-default";
						else
							defActionItem.Text = "Show all by-default";
						defActionItem.ImageIndex = 2;
					}
					defActionItem.SubItems[1].Text = GetFilterCounterString(
						filters.FilteringEnabled, filters.GetDefaultAction(), filters.GetDefaultActionCounter(), host.IsHighlightFilter);
				}

				list.Enabled = filters.FilteringEnabled;
			}
			finally
			{
				updateLock--;
				list.EndUpdate();
			}
	
		}

		public int SelectedCount
		{
			get
			{ 
				int ret = 0;
				foreach (ListViewItem i in list.SelectedItems)
					if (Get(i) != null)
						ret++;
				return ret;
			}
		}

		public IEnumerable<Filter> SelectedFilters
		{
			get
			{
				foreach (ListViewItem i in list.SelectedItems)
					if (Get(i) != null)
						yield return Get(i);
			}
		}

		public void SelectFilter(Filter f)
		{
			foreach (ListViewItem i in list.Items)
				i.Selected = Get(i) == f;
		}

		protected virtual void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			itemColumnHeader.Width = list.ClientSize.Width - counterColumnHeader.Width - 10;
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (updateLock > 0)
				return;
			Filter s = Get(e.Item);
			if (s != null && s.Enabled != e.Item.Checked)
			{
				s.Enabled = e.Item.Checked;
			}
			OnFilterChecked();
		}

		Filter GetTheOnly()
		{
			if (list.SelectedItems.Count != 1)
				return null;
			return Get(list.SelectedItems[0]);
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			Filter f = GetTheOnly();

			filterEnabledToolStripMenuItem.Enabled = f != null;
			propertiesToolStripMenuItem.Enabled = f != null;

			filterEnabledToolStripMenuItem.Checked = f == null || f.Enabled;
		}

		private void filterEnabledToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Filter f = GetTheOnly();
			if (f != null)
			{
				filterEnabledToolStripMenuItem.Checked = !filterEnabledToolStripMenuItem.Checked;
				f.Enabled = filterEnabledToolStripMenuItem.Checked;
				OnFilterChecked();
			}
		}

		private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Filter f = GetTheOnly();
			if (f == null)
				return;
			using (UI.FilterDialog dlg = new UI.FilterDialog(host))
			{
				dlg.Execute(f);
			}
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (Get(e.Index) == null)
				e.NewValue = CheckState.Checked;
		}

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			OnSelectionChanged();
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				Filter f = GetTheOnly();
				if (f == null)
					return;
				using (UI.FilterDialog dlg = new UI.FilterDialog(host))
				{
					dlg.Execute(f);
				}
			}
		}

	}

}
