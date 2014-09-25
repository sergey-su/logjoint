using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.ThreadsList
{
	public interface IViewItem
	{
		IThread Thread { get; }
		void SetSubItemText(int subItemIdx, string text);
		void SetSubItemBookmark(int subItemIdx, IBookmark bmk);
		string Text { get; set; }
		bool Checked { get; set; }
		bool Selected { get; set; }
	};

	public interface IView
	{
		void BeginBulkUpdate();
		void EndBulkUpdate();
		IEnumerable<IViewItem> Items { get; }
		void RemoveItem(IViewItem item);
		IViewItem Add(IThread thread);
		IViewItem TopItem { get; set; }
		void SortItems();
		void UpdateFocusedThreadView();
	};

	public class Presenter
	{
		#region Public interface

		public interface ICallback
		{
			void ShowLine(IBookmark bmk, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
			MessageBase FocusedMessage { get; }
			event EventHandler FocusedMessageChanged;
			void ExecuteThreadPropertiesDialog(IThread thread);
			void ForceViewUpdateAfterThreadChecked();
		};

		public Presenter(Model model, IView view, ICallback callback)
		{
			this.model = model;
			this.view = view;
			this.callback = callback;

			callback.FocusedMessageChanged += (s, e) => view.UpdateFocusedThreadView();
		}

		public void UpdateView()
		{
			Dictionary<int, IViewItem> existingThreads = new Dictionary<int, IViewItem>();
			foreach (IViewItem vi in view.Items)
			{
				existingThreads.Add(vi.Thread.GetHashCode(), vi);
			}
			BeginBulkUpdate();
			try
			{
				foreach (IViewItem vi in existingThreads.Values)
					if (vi.Thread.IsDisposed)
						view.RemoveItem(vi);

				foreach (IThread t in model.Threads.Items)
				{
					if (t.IsDisposed)
						continue;

					int hash = t.GetHashCode();
					IViewItem vi;
					if (!existingThreads.TryGetValue(hash, out vi))
					{
						vi = view.Add(t);
						existingThreads.Add(hash, vi);
					}

					vi.Text = t.DisplayName;

					vi.SetSubItemBookmark(1, t.FirstKnownMessage);
					vi.SetSubItemBookmark(2, t.LastKnownMessage);
					vi.Checked = t.ThreadMessagesAreVisible;
				}
			}
			finally
			{
				EndBulkUpdate();
			}
		}

		public void Select(IThread thread)
		{
			BeginBulkUpdate();
			try
			{
				foreach (IViewItem vi in view.Items)
				{
					vi.Selected = vi.Thread == thread;
					if (vi.Selected)
						view.TopItem = vi;
				}
			}
			finally
			{
				EndBulkUpdate();
			}
		}

		public bool IsThreadFocused(IThread thread)
		{
			var msg = callback.FocusedMessage;
			if (msg == null)
				return false;
			return msg.Thread == thread;
		}

		public void BookmarkClicked(IBookmark bmk)
		{
			callback.ShowLine(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
		}

		public void ItemChecked(IViewItem item, bool newCheckedValue)
		{
			if (updateLock != 0)
				return;
			IThread t = item.Thread;
			if (t.IsDisposed)
				return;
			if (t.LogSource != null && !t.LogSource.Visible)
				return;
			if (t.Visible == newCheckedValue)
				return;
			t.Visible = newCheckedValue;
			callback.ForceViewUpdateAfterThreadChecked();
		}

		public void ShowOnlyThisThreadClicked(IViewItem item)
		{
			IThread t = item.Thread;
			if (t.IsDisposed)
				return;
			foreach (IViewItem vi in view.Items)
			{
				if (!vi.Thread.IsDisposed)
					vi.Thread.Visible = (item == vi);
			}
			callback.ForceViewUpdateAfterThreadChecked();
		}

		public void ShowAllThreadsClicked()
		{
			bool updateNeeded = false;
			foreach (IViewItem vi in view.Items)
			{
				if (!vi.Thread.IsDisposed && !vi.Thread.Visible)
				{
					updateNeeded = true;
					vi.Thread.Visible = true;
				}
			}
			if (updateNeeded)
			{
				callback.ForceViewUpdateAfterThreadChecked();
			}
		}

		public bool ItemIsAboutToBeChecked(IViewItem item)
		{
			if (updateLock != 0)
				return true;
			var t = item.Thread;
			if (t.IsDisposed || (t.LogSource != null && !t.LogSource.Visible))
				return false;
			return true;
		}

		public void VisibilityMenuItemClicked(IViewItem item)
		{
		}

		public void ThreadPropertiesMenuItemClicked(IViewItem item)
		{
			if (item.Thread.IsDisposed)
				return;
			callback.ExecuteThreadPropertiesDialog(item.Thread);
		}

		public void ListColumnClicked(int column)
		{
			if (column == sortColumn)
			{
				ascending = !ascending;
			}
			else
			{
				sortColumn = column;
			}
			view.SortItems();
		}

		public struct SortingInfo
		{
			public int SortColumn;
			public bool Ascending;
		};

		public SortingInfo? GetSortingInfo()
		{
			if (sortColumn < 0)
				return null;
			return new SortingInfo() { SortColumn = sortColumn, Ascending = ascending };
		}

		public int CompareThreads(IThread t1, IThread t2)
		{
			if (t1.IsDisposed || t2.IsDisposed)
				return 0;
			int ret = 0;
			switch (sortColumn)
			{
				case 0:
					ret = string.Compare(t2.ID, t1.ID);
					break;
				case 1:
					ret = MessageTimestamp.Compare(GetBookmarkDate(t2.FirstKnownMessage), GetBookmarkDate(t1.FirstKnownMessage));
					break;
				case 2:
					ret = MessageTimestamp.Compare(GetBookmarkDate(t2.LastKnownMessage), GetBookmarkDate(t1.LastKnownMessage));
					break;
			}
			return ascending ? ret : -ret;
		}

		#endregion

		#region Implementation

		void BeginBulkUpdate()
		{
			++updateLock;
			view.BeginBulkUpdate();
		}

		void EndBulkUpdate()
		{
			view.EndBulkUpdate();
			--updateLock;
		}

		static MessageTimestamp GetBookmarkDate(IBookmark bmk)
		{
			return bmk != null ? bmk.Time : MessageTimestamp.MinValue;
		}

		readonly Model model;
		readonly IView view;
		readonly ICallback callback;
		int updateLock = 0;
		int sortColumn = -1;
		bool ascending = false;

		#endregion
	};
};