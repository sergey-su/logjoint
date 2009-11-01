using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl
	{
		ISourcesListViewHost host;
		int updateLock;

		public SourcesListView()
		{
			InitializeComponent();
		}

		public void SetHost(ISourcesListViewHost host)
		{
			this.host = host;
		}

		public event EventHandler SelectionChanged;

		public void Select(ILogSource src)
		{
			list.BeginUpdate();
			try
			{
				foreach (ListViewItem lvi in list.Items)
				{
					lvi.Selected = Get(lvi) == src;
					if (lvi.Selected)
						list.TopItem = lvi;
				}
			}
			finally
			{
				list.EndUpdate();
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
					ILogSource ls = Get(i);
					if (ls.IsDisposed)
						list.Items.RemoveAt(i);
				}
				foreach (ILogSource s in host.LogSources)
				{
					ListViewItem lvi;
					int idx = list.Items.IndexOfKey(s.GetHashCode().ToString());
					if (idx < 0)
					{
						lvi = new ListViewItem();
						lvi.Tag = s;
						lvi.Name = s.GetHashCode().ToString();
						list.Items.Add(lvi);
					}
					else
					{
						lvi = list.Items[idx];
					}

					lvi.Checked = s.Visible;

					StringBuilder msg = new StringBuilder();
					LogReaderStats stats = s.Reader.Stats;
					switch (stats.State)
					{
						case ReaderState.NoFile:
							msg.Append("(No trace file)");
							break;
						case ReaderState.DetectingAvailableTime:
							msg.AppendFormat("Processing... {0}", s.DisplayName);
							break;
						case ReaderState.LoadError:
							msg.AppendFormat(
								"{0}: loading failed ({1})", 
								s.DisplayName,
								stats.Error != null ? stats.Error.Message : "");
							break;
						case ReaderState.Loading:
							msg.AppendFormat("{0}: loading ({1} messages loaded)", s.DisplayName, stats.MessagesCount);
							break;
						case ReaderState.Idle:
							msg.AppendFormat("{0}: idle ({1} messages in memory", s.DisplayName, stats.MessagesCount);
							if (stats.LoadedBytes != null)
							{
								msg.Append(", ");
								if (stats.TotalBytes != null)
								{
									FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
									msg.Append(" of ");
									FormatBytesUserFriendly(stats.TotalBytes.Value, msg);
								}
								else
								{
									FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
								}
							}
							msg.Append(")");
							break;
					}
					lvi.Text = msg.ToString();
					if (stats.Error != null)
						lvi.BackColor = Color.FromArgb(255, 128, 128);
					else
						lvi.BackColor = s.Color;
				}
			}
			finally
			{
				updateLock--;
				list.EndUpdate();
			}
		}

		static readonly string[] bytesUnits = new string[] { "B", "KB", "MB", "GB", "TB" };

		static void FormatBytesUserFriendly(long bytes, StringBuilder outBuffer)
		{
			long divisor = 1;
			int unitIdx = 0;
			int maxUnitIdx = bytesUnits.Length - 1;
			for (; ; )
			{
				if (bytes / divisor < 1024 || unitIdx == maxUnitIdx)
				{
					if (divisor == 1)
						outBuffer.Append(bytes);
					else
						outBuffer.AppendFormat("{0:0.0}", (double)bytes / (double)divisor);
					outBuffer.AppendFormat(" {0}", bytesUnits[unitIdx]);
					break;
				}
				else
				{
					divisor *= 1024;
					++unitIdx;
				}
			}
		}

		public IEnumerable<ILogSource> SelectedSources
		{
			get
			{
				foreach (ListViewItem i in list.SelectedItems)
					yield return Get(i);
			}
		}

		public int SelectedCount
		{
			get { return list.SelectedItems.Count; }
		}

		ILogSource Get(int i)
		{
			return Get(list.Items[i]);
		}

		ILogSource Get(ListViewItem i)
		{
			return i.Tag as ILogSource;
		}

		ILogSource Get()
		{
			foreach (ListViewItem lvi in list.SelectedItems)
				return Get(lvi);
			return null;
		}

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			itemColumnHeader.Width = list.ClientSize.Width - 10;
		}

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			OnSelectionChanged();
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (updateLock > 0)
				return;
			ILogSource s = Get(e.Item);
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
			ILogSource s = Get();
			if (s == null)
			{
				e.Cancel = true;
			}
			else
			{
				sourceVisisbleMenuItem.Checked = s.Visible;
			}

			// Not implemented yet
			sourceProprtiesMenuItem.Visible = false;
		}

		private void threadProprtiesMenuItem_Click(object sender, EventArgs e)
		{
			ILogSource src = Get();
			if (src == null)
				return;
			using (SourceDetailsForm f = new SourceDetailsForm(src))
			{
				f.ShowDialog();
			}
		}

		private void sourceVisisbleMenuItem_Click(object sender, EventArgs e)
		{
			if (updateLock != 0)
				return;
			ILogSource s = Get();
			if (s == null)
				return;
			sourceVisisbleMenuItem.Checked = !sourceVisisbleMenuItem.Checked;
			s.Visible = sourceVisisbleMenuItem.Checked;
		}
	}
	
	public interface ISourcesListViewHost
	{
		IEnumerable<ILogSource> LogSources { get; }
	};
}
