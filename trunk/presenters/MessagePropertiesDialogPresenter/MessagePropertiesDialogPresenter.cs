using System;
using System.Linq;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
	public class Presenter : IPresenter, IViewEvents, IMessagePropertiesFormHost
	{
		public Presenter(
			IBookmarks bookmarks,
			IFiltersList hlFilters,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			IPresentersFacade navHandler)
		{
			this.hlFilters = hlFilters;
			this.bookmarks = bookmarks;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.navHandler = navHandler;

			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				if (GetPropertiesForm() != null)
					GetPropertiesForm().UpdateView(viewerPresenter.FocusedMessage);
			};
			bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				var focused = viewerPresenter.FocusedMessage;
				if (GetPropertiesForm() != null && focused != null)
				{
					if (args.AffectedBookmarks.Any(b => b.Position == focused.Position))
						GetPropertiesForm().UpdateView(focused);
				}
			};
		}

		void IPresenter.ShowDialog()
		{
			if (GetPropertiesForm() == null)
			{
				propertiesForm = view.CreateDialog(this);
			}
			propertiesForm.UpdateView(viewerPresenter.FocusedMessage);
			propertiesForm.Show();
		}

		IPresentersFacade IMessagePropertiesFormHost.UINavigationHandler
		{
			get { return navHandler; }
		}

		bool IMessagePropertiesFormHost.BookmarksSupported
		{
			get { return bookmarks != null; }
		}

		bool IMessagePropertiesFormHost.IsMessageBookmarked(IMessage msg)
		{
			return bookmarks != null && bookmarks.GetMessageBookmarks(msg).Length > 0;
		}

		bool IMessagePropertiesFormHost.NavigationOverHighlightedIsEnabled
		{
			get
			{
				return hlFilters.FilteringEnabled && hlFilters.Count > 0;
			}
		}

		void IMessagePropertiesFormHost.ToggleBookmark(IMessage msg)
		{
			var msgBmks = bookmarks.GetMessageBookmarks(msg);
			if (msgBmks.Length == 0)
				bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(msg, 0));
			else foreach (var b in msgBmks)
				bookmarks.ToggleBookmark(b);
		}

		void IMessagePropertiesFormHost.ShowLine(IBookmark msg, BookmarkNavigationOptions options)
		{
			navHandler.ShowMessage(msg, options);
		}

		void IMessagePropertiesFormHost.Next()
		{
			viewerPresenter.GoToNextMessage().IgnoreCancellation();
		}

		void IMessagePropertiesFormHost.Prev()
		{
			viewerPresenter.GoToPrevMessage().IgnoreCancellation();
		}

		void IMessagePropertiesFormHost.NextHighlighted()
		{
			viewerPresenter.GoToNextHighlightedMessage().IgnoreCancellation();
		}

		void IMessagePropertiesFormHost.PrevHighlighted()
		{
			viewerPresenter.GoToPrevHighlightedMessage().IgnoreCancellation();
		}

		#region Implementation

		IDialog GetPropertiesForm()
		{
			if (propertiesForm != null)
				if (propertiesForm.IsDisposed)
					propertiesForm = null;
			return propertiesForm;
		}


		readonly IFiltersList hlFilters;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly IPresentersFacade navHandler;
		IDialog propertiesForm;

		#endregion
	};
};