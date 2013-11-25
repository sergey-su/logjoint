using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public interface IView
	{
		void UpdateItems(IEnumerable<KeyValuePair<IBookmark, TimeSpan?>> items);
		void RefreshFocusedMessageMark();
		IBookmark SelectedBookmark { get; }
	};

	public class Presenter
	{
		#region Public interface

		public Presenter(Model model, IView view)
		{
			this.model = model;
			this.view = view;
		}

		public delegate void BookmarkEvent(Presenter sender, IBookmark bmk);

		public event BookmarkEvent Click;

		public void UpdateView()
		{
			view.UpdateItems(EnumBookmarkForView());
			UpdateFocusedMessagePosition();
		}

		IEnumerable<KeyValuePair<IBookmark, TimeSpan?>> EnumBookmarkForView()
		{
			DateTime? prevTimestamp = null;
			foreach (IBookmark bmk in model.Bookmarks.Items)
			{
				var currTimestamp = bmk.Time.ToUniversalTime();
				yield return new KeyValuePair<IBookmark, TimeSpan?>(bmk, prevTimestamp != null ? currTimestamp - prevTimestamp.Value : new TimeSpan?());
				prevTimestamp = currTimestamp;
			}
		}

		public void HandleEnterKey()
		{
			ClickSelectedLink();
		}

		public void ViewDoubleClicked()
		{
			ClickSelectedLink();
		}

		public void BookmarkLeftClicked(IBookmark bmk)
		{
			NavigateTo(bmk);
		}

		public void DeleteMenuItemClicked()
		{
			var bmk = view.SelectedBookmark;
			if (bmk != null)
			{
				model.Bookmarks.ToggleBookmark(bmk);
				UpdateView();
			}
		}

		public bool ContextMenuMenuCanBeShown { get { return view.SelectedBookmark != null; } }

		public IBookmarks Bookmarks
		{
			get { return model.Bookmarks; }
		}

		public Tuple<int, int> FocusedMessagePosition { get { return focusedMessagePosition; } }

		public void SetMasterFocusedMessage(MessageBase value)
		{
			if (focusedMessage == value)
				return;
			focusedMessage = value;
			UpdateFocusedMessagePosition();
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

		public void NavigateTo(IBookmark bmk)
		{
			if (Click != null)
				Click(this, bmk);
		}

		#endregion

		#region Implementation

		void ClickSelectedLink()
		{
			var bmk = view.SelectedBookmark;
			if (bmk != null)
				NavigateTo(bmk);
		}

		Tuple<int, int> FindFocusedMessagePosition()
		{
			if (focusedMessage == null)
				return null;
			return model.Bookmarks.FindBookmark(new Bookmark(focusedMessage));
		}

		readonly Model model;
		readonly IView view;
		MessageBase focusedMessage;
		Tuple<int, int> focusedMessagePosition;

		#endregion
	};
};