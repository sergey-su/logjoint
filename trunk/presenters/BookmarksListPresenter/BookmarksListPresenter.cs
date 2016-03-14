using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public class Presenter : IPresenter, IViewEvents, IPresentationDataAccess
	{
		#region Public interface

		public Presenter(
			IModel model, IView view, 
			IHeartBeatTimer heartbeat,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IClipboardAccess clipboardAccess)
		{
			this.model = model;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.clipboardAccess = clipboardAccess;

			model.Bookmarks.OnBookmarksChanged += (sender, evt) => updateTracker.Invalidate();
			heartbeat.OnTimer += (sender, evt) =>
			{
				if (evt.IsNormalUpdate && updateTracker.Validate())
					UpdateViewInternal(null, ViewUpdateFlags.None);
			};
			model.SourcesManager.OnLogSourceVisiblityChanged += (sender, evt) => updateTracker.Invalidate();
			loadedMessagesPresenter.LogViewerPresenter.ColoringModeChanged += (sender, evt) => view.Invalidate();

			view.SetPresenter(this);
		}

		public event BookmarkEvent Click;

		void IPresenter.SetMasterFocusedMessage(IMessage value)
		{
			if (focusedMessage == value)
				return;
			focusedMessage = value;
			UpdateFocusedMessagePosition();
		}

		void IPresenter.DeleteSelectedBookmarks()
		{
			DeleteSelectedBookmarks();
		}

		void IViewEvents.OnEnterKeyPressed()
		{
			ClickSelectedLink(focusMessagesView: false);
		}

		void IViewEvents.OnViewDoubleClicked()
		{
			ClickSelectedLink(focusMessagesView: true);
		}

		void IViewEvents.OnBookmarkLeftClicked(IBookmark bmk)
		{
			NavigateTo(bmk);
		}

		void IViewEvents.OnMenuItemClicked(ContextMenuItem item)
		{
			if (item == ContextMenuItem.Delete)
				DeleteSelectedBookmarks();
			else if (item == ContextMenuItem.Copy)
				CopyToClipboard(copyTimeDeltas: false);
			else if (item == ContextMenuItem.CopyWithDeltas)
				CopyToClipboard(copyTimeDeltas: true);
		}

		ContextMenuItem IViewEvents.OnContextMenu()
		{
			var ret = ContextMenuItem.None;
			var selectedCount = view.SelectedBookmarks.Count();
			if (selectedCount > 0)
				ret |= (ContextMenuItem.Delete | ContextMenuItem.Copy);
			if (selectedCount > 1)
				ret |= ContextMenuItem.CopyWithDeltas;
			return ret;
		}

		void IViewEvents.OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition)
		{
			focusedMessagePosition = this.focusedMessagePosition;
		}

		void IViewEvents.OnCopyShortcutPressed()
		{
			CopyToClipboard(copyTimeDeltas: false);
		}

		void IViewEvents.OnDeleteButtonPressed()
		{
			DeleteSelectedBookmarks();
		}

		void IViewEvents.OnSelectAllShortcutPressed()
		{
			view.UpdateItems(EnumBookmarkForView(model.Bookmarks.Items.ToLookup(b => b)), 
				ViewUpdateFlags.ItemsCountDidNotChange);
		}

		void IViewEvents.OnSelectionChanged()
		{
			var flags = 
				ViewUpdateFlags.ItemsCountDidNotChange 
				| ViewUpdateFlags.SelectionDidNotChange; // items already selected in view did not change their selection
			UpdateViewInternal(null, flags);
		}

		Appearance.ColoringMode IPresentationDataAccess.Coloring
		{
			get { return loadedMessagesPresenter.LogViewerPresenter.Coloring; }
		}

		#endregion

		#region Implementation

		void NavigateTo(IBookmark bmk)
		{
			if (Click != null)
				Click(this, bmk);
		}

		void ClickSelectedLink(bool focusMessagesView)
		{
			var bmk = view.SelectedBookmark;
			if (bmk != null)
			{
				NavigateTo(bmk);
				if (focusMessagesView)
					loadedMessagesPresenter.Focus();
			}
		}

		Tuple<int, int> FindFocusedMessagePosition()
		{
			if (focusedMessage == null)
				return null;
			return model.Bookmarks.FindBookmark(model.Bookmarks.Factory.CreateBookmark(focusedMessage));
		}

		void UpdateViewInternal(IEnumerable<IBookmark> newSelection, ViewUpdateFlags flags)
		{
			view.UpdateItems(EnumBookmarkForView(
				newSelection != null ? newSelection.ToLookup(b => b) : view.SelectedBookmarks.ToLookup(b => b)), flags);
			UpdateFocusedMessagePosition();
		}

		IEnumerable<ViewItem> EnumBookmarkForView(ILookup<IBookmark, IBookmark> selected)
		{
			return EnumBookmarkForView(model.Bookmarks.Items, selected);
		}

		static IEnumerable<ViewItem> EnumBookmarkForView(IEnumerable<IBookmark> bookmarks, ILookup<IBookmark, IBookmark> selected)
		{
			DateTime? prevTimestamp = null;
			DateTime? prevSelectedTimestamp = null;
			bool multiSelection = selected.Count >= 2;
			foreach (IBookmark bmk in bookmarks)
			{
				var ts = bmk.Time.ToUniversalTime();
				var ls = bmk.GetLogSource();
				var isEnabled = ls != null && ls.Visible;
				var isSelected = selected.Contains(bmk);
				var deltaBase = multiSelection ? (isSelected ? prevSelectedTimestamp : null) : prevTimestamp;
				var delta = deltaBase != null ? ts - deltaBase.Value : new TimeSpan?();
				var altDelta = prevTimestamp != null ? ts - prevTimestamp.Value : new TimeSpan?();
				yield return new ViewItem()
				{
					Bookmark = bmk,
					Delta = TimeUtils.TimeDeltaToString(delta),
					AltDelta = TimeUtils.TimeDeltaToString(altDelta),
					IsSelected = isSelected,
					IsEnabled = isEnabled
				};
				prevTimestamp = ts;
				if (isSelected)
					prevSelectedTimestamp = ts;
			}
		}

		private void UpdateFocusedMessagePosition()
		{
			var newFocusedMessagePosition = FindFocusedMessagePosition();
			bool updateFocusedMessagePosition = false;
			if ((newFocusedMessagePosition != null) != (focusedMessagePosition != null))
				updateFocusedMessagePosition = true;
			else if (newFocusedMessagePosition != null && focusedMessagePosition != null)
				if (newFocusedMessagePosition.Item1 != focusedMessagePosition.Item1 || newFocusedMessagePosition.Item2 != focusedMessagePosition.Item2)
					updateFocusedMessagePosition = true;
			if (updateFocusedMessagePosition)
			{
				focusedMessagePosition = newFocusedMessagePosition;
				view.RefreshFocusedMessageMark();
			}
		}

		private void DeleteSelectedBookmarks()
		{
			var selectedBmks = view.SelectedBookmarks.ToLookup(b => b);
			if (selectedBmks.Count == 0)
				return;
			IBookmark newSelectionCandidate2 = null;
			IBookmark newSelectionCandidate1 = null;
			bool passedSelection = false;
			foreach (var b in model.Bookmarks.Items)
			{
				if (selectedBmks.Contains(b))
					passedSelection = true;
				else if (!passedSelection)
					newSelectionCandidate2 = b;
				else if (newSelectionCandidate1 == null)
					newSelectionCandidate1 = b;
			}
			foreach (var bmk in selectedBmks.SelectMany(g => g))
				model.Bookmarks.ToggleBookmark(bmk);
			UpdateViewInternal(new[] { newSelectionCandidate1 ?? newSelectionCandidate2 }.Where(c => c != null), ViewUpdateFlags.None);
		}

		private void CopyToClipboard(bool copyTimeDeltas)
		{
			var texts = 
				EnumBookmarkForView(view.SelectedBookmarks, new IBookmark[0].ToLookup(b => b))
				.Select(b => new 
				{ 
					Delta = copyTimeDeltas ? b.Delta : "",
					Text = (b.Bookmark.MessageText ?? b.Bookmark.DisplayName) ?? "",
					Bookmark = b.Bookmark
				})
				.ToArray();
			if (texts.Length == 0)
				return;
			var maxDeltasLen = texts.Max(b => b.Delta.Length);
	
			var textToCopy = new StringBuilder();
			foreach (var b in texts)
			{
				if (copyTimeDeltas)
					textToCopy.AppendFormat("{0,-"+maxDeltasLen.ToString()+"}\t", b.Delta);
				textToCopy.AppendLine(b.Text);
			}

			var htmlToCopy = new StringBuilder();
			htmlToCopy.Append("<div style='font-size:8pt; font-family: monospace; white-space: pre-wrap;'>");
			foreach (var b in texts)
			{
				htmlToCopy.AppendFormat("<font style='background: {0}'>", GetBackgroundColorAsHtml(b.Bookmark));
				if (copyTimeDeltas)
					htmlToCopy.AppendFormat("{0,-" + maxDeltasLen.ToString() + "}\t", b.Delta);
				htmlToCopy.Append(System.Security.SecurityElement.Escape(b.Text));
				htmlToCopy.AppendLine("</font>");
			}
			htmlToCopy.Append("</div><br/>");

			if (textToCopy.Length > 0)
			{
				clipboardAccess.SetClipboard(textToCopy.ToString(), htmlToCopy.ToString());
			}
		}

		string GetBackgroundColorAsHtml(IBookmark b)
		{
			var coloring = loadedMessagesPresenter.LogViewerPresenter.Coloring;
			var cl = "black";
			if (coloring == Settings.Appearance.ColoringMode.Threads)
			{
				var t = b.GetSafeThread();
				if (t != null)
					cl = t.ThreadColor.ToHtmlColor();
			}
			else if (coloring == Settings.Appearance.ColoringMode.Sources)
			{
				var ls = b.GetSafeLogSource();
				if (ls != null)
					cl = ls.Color.ToHtmlColor();
			}
			return cl;
		}

		readonly IModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly IClipboardAccess clipboardAccess;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
		IMessage focusedMessage;
		Tuple<int, int> focusedMessagePosition;

		#endregion
	};
};