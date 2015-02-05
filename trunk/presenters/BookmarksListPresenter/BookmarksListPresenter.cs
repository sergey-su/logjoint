using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public class Presenter : IPresenter, IViewEvents
	{
		#region Public interface

		public Presenter(IModel model, IView view, IHeartBeatTimer heartbeat, LoadedMessages.IPresenter loadedMessagesPresenter)
		{
			this.model = model;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;

			model.Bookmarks.OnBookmarksChanged += (sender, evt) => updateTracker.Invalidate();
			heartbeat.OnTimer += (sender, evt) =>
			{
				if (evt.IsNormalUpdate && updateTracker.Validate())
					UpdateViewInternal();
			};

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

		void IViewEvents.OnDeleteMenuItemClicked()
		{
			DeleteDelectedBookmarks();
		}

		void IViewEvents.OnContextMenu(ref bool cancel)
		{
			cancel = view.SelectedBookmark == null;
		}

		void IViewEvents.OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition)
		{
			focusedMessagePosition = this.focusedMessagePosition;
		}

		void IViewEvents.OnCopyShortcutPressed()
		{
			var textToCopy = new StringBuilder();
			foreach (var b in view.SelectedBookmarks)
				textToCopy.AppendLine((b.MessageText ?? b.DisplayName) ?? "");
			if (textToCopy.Length > 0)
				view.SetClipboard(textToCopy.ToString());
		}

		void IViewEvents.OnDeleteButtonPressed()
		{
			DeleteDelectedBookmarks();
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

		void UpdateViewInternal()
		{
			view.UpdateItems(EnumBookmarkForView(view.SelectedBookmarks.ToLookup(b => b)));
			UpdateFocusedMessagePosition();
		}

		IEnumerable<ViewItem> EnumBookmarkForView(ILookup<IBookmark, IBookmark> selected)
		{
			DateTime? prevTimestamp = null;
			foreach (IBookmark bmk in model.Bookmarks.Items)
			{
				var currTimestamp = bmk.Time.ToUniversalTime();
				yield return new ViewItem()
				{
					Bookmark = bmk,
					Delta = prevTimestamp != null ? currTimestamp - prevTimestamp.Value : new TimeSpan?(),
					IsSelected = selected.Contains(bmk)
				};
				prevTimestamp = currTimestamp;
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

		private void DeleteDelectedBookmarks()
		{
			bool changed = false;
			foreach (var bmk in view.SelectedBookmarks.ToList())
			{
				model.Bookmarks.ToggleBookmark(bmk);
				changed = true;
			}
			if (changed)
				UpdateViewInternal();
		}

		readonly IModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
		IMessage focusedMessage;
		Tuple<int, int> focusedMessagePosition;

		#endregion
	};
};