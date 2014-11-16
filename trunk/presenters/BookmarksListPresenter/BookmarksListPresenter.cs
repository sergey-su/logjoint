using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public class Presenter : IPresenter, IPresenterEvents
	{
		#region Public interface

		public Presenter(IModel model, IView view, IHeartBeatTimer heartbeat)
		{
			this.model = model;
			this.view = view;

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

		void IPresenterEvents.OnEnterKeyPressed()
		{
			ClickSelectedLink();
		}

		void IPresenterEvents.OnViewDoubleClicked()
		{
			ClickSelectedLink();
		}

		void IPresenterEvents.OnBookmarkLeftClicked(IBookmark bmk)
		{
			NavigateTo(bmk);
		}

		void IPresenterEvents.OnDeleteMenuItemClicked()
		{
			var bmk = view.SelectedBookmark;
			if (bmk != null)
			{
				model.Bookmarks.ToggleBookmark(bmk);
				UpdateViewInternal();
			}
		}

		void IPresenterEvents.OnContextMenu(ref bool cancel)
		{
			cancel = view.SelectedBookmark == null;
		}

		void IPresenterEvents.OnFocusedMessagePositionRequired(out Tuple<int, int> focusedMessagePosition)
		{
			focusedMessagePosition = this.focusedMessagePosition;
		}

		#endregion

		#region Implementation

		void NavigateTo(IBookmark bmk)
		{
			if (Click != null)
				Click(this, bmk);
		}

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
			return model.Bookmarks.FindBookmark(model.Bookmarks.Factory.CreateBookmark(focusedMessage));
		}

		void UpdateViewInternal()
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

		readonly IModel model;
		readonly IView view;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
		IMessage focusedMessage;
		Tuple<int, int> focusedMessagePosition;

		#endregion
	};
};