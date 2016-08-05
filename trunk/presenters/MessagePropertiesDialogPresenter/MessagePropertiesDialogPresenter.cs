using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
	public class Presenter : IPresenter, IViewEvents, IMessagePropertiesFormHost
	{
		public Presenter(
			IModel model,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			IPresentersFacade navHandler)
		{
			this.model = model;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.navHandler = navHandler;

			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				if (GetPropertiesForm() != null)
					GetPropertiesForm().UpdateView(viewerPresenter.FocusedMessage);
			};
			model.Bookmarks.OnBookmarksChanged += (sender, args) =>
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
			get { return model.Bookmarks != null; }
		}

		bool IMessagePropertiesFormHost.IsMessageBookmarked(IMessage msg)
		{
			return model.Bookmarks != null && model.Bookmarks.IsBookmarked(msg);
		}

		bool IMessagePropertiesFormHost.NavigationOverHighlightedIsEnabled
		{
			get
			{
				return model.HighlightFilters.FilteringEnabled && model.HighlightFilters.Count > 0;
			}
		}

		void IMessagePropertiesFormHost.ToggleBookmark(IMessage line)
		{
			viewerPresenter.ToggleBookmark(line);
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


		readonly IModel model;
		readonly IView view;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly IPresentersFacade navHandler;
		IDialog propertiesForm;

		#endregion
	};
};