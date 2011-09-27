using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;
using ILogSourcePreprocessing = LogJoint.Preprocessing.ILogSourcePreprocessing;
using LogJoint.UI.Presenters.SourcesList;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl, IView
	{
		Presenter presenter;
		ISourcesListViewHost host;
		int updateLock;
		SourceDetailsForm openSourceDetailsDialog;
		bool refreshColumnHeaderPosted;
		static readonly Color failedSourceColor = Color.FromArgb(255, 128, 128);

		public SourcesListView()
		{
			InitializeComponent();
			this.DoubleBuffered = true;
		}

		public void SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;
		}

		public Presenter Presenter { get { return presenter; } }


		public event EventHandler DeleteRequested;

		public void SetHost(ISourcesListViewHost host)
		{
			this.host = host;
		}

		public void InvalidateFocusedMessageArea()
		{
			list.Invalidate(new Rectangle(0, 0, 5, Height));
		}

		public event EventHandler SelectionChanged;

		public void Select(ILogSource src)
		{
			list.BeginUpdate();
			try
			{
				foreach (ListViewItem lvi in list.Items)
				{
					lvi.Selected = GetLogSource(lvi) == src;
					if (lvi.Selected)
						list.TopItem = lvi;
				}
			}
			finally
			{
				list.EndUpdate();
			}
		}

		struct ItemData
		{
			public int HashCode;
			public object ItemObject;
			public bool Checked;
			public string Description;
			public Color ItemColor;
		};

		IEnumerable<ItemData> EnumItemsData()
		{
			foreach (ILogSource s in host.LogSources)
			{
				StringBuilder msg = new StringBuilder();
				LogProviderStats stats = s.Provider.Stats;
				switch (stats.State)
				{
					case LogProviderState.NoFile:
						msg.Append("(No trace file)");
						break;
					case LogProviderState.DetectingAvailableTime:
						msg.AppendFormat("Processing... {0}", s.DisplayName);
						break;
					case LogProviderState.LoadError:
						msg.AppendFormat(
							"{0}: loading failed ({1})",
							s.DisplayName,
							stats.Error != null ? stats.Error.Message : "");
						break;
					case LogProviderState.Loading:
						msg.AppendFormat("{0}: loading ({1} messages loaded)", s.DisplayName, stats.MessagesCount);
						break;
					case LogProviderState.Idle:
						msg.AppendFormat("{0} ({1} messages in memory", s.DisplayName, stats.MessagesCount);
						if (stats.LoadedBytes != null)
						{
							msg.Append(", ");
							if (stats.TotalBytes != null)
							{
								StringUtils.FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
								msg.Append(" of ");
								StringUtils.FormatBytesUserFriendly(stats.TotalBytes.Value, msg);
							}
							else
							{
								StringUtils.FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
							}
						}
						msg.Append(")");
						break;
				}
				Color color;
				if (stats.Error != null)
					color = failedSourceColor;
				else
					color = s.Color.ToColor();
				yield return new ItemData() 
				{ 
					HashCode = s.GetHashCode(), ItemObject = s,
					Checked = s.Visible, Description = msg.ToString(),
					ItemColor = color
				};
			}
			foreach (ILogSourcePreprocessing pls in host.LogSourcePreprocessings)
			{
				string description = pls.CurrentStepDescription;
				if (pls.Failure != null)
					description = string.Format("{0}. Error: {1}", description, pls.Failure.Message);
				yield return new ItemData()
				{
					HashCode = pls.GetHashCode(),
					ItemObject = pls,
					Checked = true,
					Description =  description,
					ItemColor = pls.Failure == null ? Color.White : failedSourceColor
				};
			}
		}

		public void UpdateView()
		{		
			updateLock++;
			list.BeginUpdate();
			try
			{
				for (int i = list.Items.Count - 1; i >= 0; --i)
				{
					ILogSource ls = GetLogSource(i);
					if (ls != null)
					{
						if (ls.IsDisposed)
							list.Items.RemoveAt(i);
						continue;
					}
					ILogSourcePreprocessing pls = GetLogSourcePreprocessing(i);
					if (pls != null)
					{
						if (pls.IsDisposed)
							list.Items.RemoveAt(i);
						continue;
					}
				}
				foreach (var item in EnumItemsData())
				{
					ListViewItem lvi;
					int idx = list.Items.IndexOfKey(item.HashCode.ToString());
					if (idx < 0)
					{
						lvi = new ListViewItem();
						lvi.Tag = item.ItemObject;
						lvi.Name = item.HashCode.ToString();
						list.Items.Add(lvi);
					}
					else
					{
						lvi = list.Items[idx];
					}

					lvi.Checked = item.Checked;
					lvi.Text = item.Description;
					lvi.BackColor = item.ItemColor;
				}
				if (openSourceDetailsDialog != null)
				{
					openSourceDetailsDialog.UpdateView();
				}
			}
			finally
			{
				updateLock--;
				list.EndUpdate();
			}
		}

		public IEnumerable<ILogSource> SelectedSources
		{
			get
			{
				foreach (ListViewItem i in list.SelectedItems)
				{
					var ls = GetLogSource(i);
					if (ls != null)
						yield return ls;
				}
			}
		}

		public IEnumerable<ILogSourcePreprocessing> SelectedPreprocessings
		{
			get
			{
				foreach (ListViewItem i in list.SelectedItems)
				{
					var p = GetLogSourcePreprocessing(i);
					if (p != null)
						yield return p;
				}
			}
		}

		public int SelectedCount
		{
			get { return list.SelectedItems.Count; }
		}

		ILogSource GetLogSource(int i)
		{
			return GetLogSource(list.Items[i]);
		}

		ILogSource GetLogSource(ListViewItem i)
		{
			return i.Tag as ILogSource;
		}

		ILogSource GetLogSource()
		{
			foreach (ListViewItem lvi in list.SelectedItems)
				return GetLogSource(lvi);
			return null;
		}

		ILogSourcePreprocessing GetLogSourcePreprocessing(int i)
		{
			return GetLogSourcePreprocessing(list.Items[i]);
		}

		ILogSourcePreprocessing GetLogSourcePreprocessing(ListViewItem i)
		{
			return i.Tag as ILogSourcePreprocessing;
		}

		static class Native
		{
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
			public const int WM_USER = 0x0400;
		}
		public const int WM_REFRESHCULUMNHEADER = Native.WM_USER + 502;

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			if (!refreshColumnHeaderPosted)
			{
				Native.PostMessage(this.Handle, WM_REFRESHCULUMNHEADER, IntPtr.Zero, IntPtr.Zero);
				refreshColumnHeaderPosted = true;
			}
		}

		void RefreshColumnHeader()
		{
			itemColumnHeader.Width = list.ClientSize.Width - 10;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_REFRESHCULUMNHEADER)
			{
				refreshColumnHeaderPosted = false;
				RefreshColumnHeader();
				return;
			}
			base.WndProc(ref m);
		}

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			OnSelectionChanged();
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (updateLock > 0)
				return;
			ILogSource s = GetLogSource(e.Item);
			if (s != null && s.Visible != e.Item.Checked)
			{
				s.Visible = e.Item.Checked;
			}
		}

		protected virtual void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ILogSource s = GetLogSource();
			if (s == null)
			{
				e.Cancel = true;
			}
			else
			{
				sourceVisisbleMenuItem.Checked = s.Visible;
				saveLogAsToolStripMenuItem.Visible = (s.Provider is ISaveAs) && ((ISaveAs)s.Provider).IsSavableAs;
			}
		}

		void ExecutePropsDialog()
		{
			ILogSource src = GetLogSource();
			if (src == null)
				return;
			using (SourceDetailsForm f = new SourceDetailsForm(src, host.UINavigationHandler))
			{
				openSourceDetailsDialog = f;
				try
				{
					f.ShowDialog();
				}
				finally
				{
					openSourceDetailsDialog = null;
				}
			}
		}

		private void sourceProprtiesMenuItem_Click(object sender, EventArgs e)
		{
			ExecutePropsDialog();
		}

		private void sourceVisisbleMenuItem_Click(object sender, EventArgs e)
		{
			if (updateLock != 0)
				return;
			ILogSource s = GetLogSource();
			if (s == null)
				return;
			sourceVisisbleMenuItem.Checked = !sourceVisisbleMenuItem.Checked;
			s.Visible = sourceVisisbleMenuItem.Checked;
		}

		private void list_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			var ls = GetLogSource(e.Item);
			if (ls != null && ls == host.FocusedMessageSource)
			{
				UIUtils.DrawFocusedItemMark(e.Graphics, e.Bounds.X + 1, (e.Bounds.Top + e.Bounds.Bottom) / 2);
			}
			e.DrawDefault = true;
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				ExecutePropsDialog();
			}
			else if (e.KeyCode == Keys.Delete)
			{
				if (DeleteRequested != null)
					DeleteRequested(this, EventArgs.Empty);
			}
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (GetLogSourcePreprocessing(e.Index) != null)
				e.NewValue = CheckState.Checked;
		}

		private void saveLogAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (GetLogSource() != null)
				host.UINavigationHandler.SaveLogSourceAs(GetLogSource());
		}
	}
	
}
