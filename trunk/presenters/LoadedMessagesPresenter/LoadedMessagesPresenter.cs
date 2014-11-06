using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class Presenter: IPresenter
	{
		readonly IModel model;
		readonly IView view;
		readonly LogViewer.Presenter messagesPresenter;
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();

		public Presenter(
			IModel model,
			IView view,
			IUINavigationHandler navHandler,
			IHeartBeatTimer heartbeat
		)
		{
			this.model = model;
			this.view = view;
			this.messagesPresenter = new Presenters.LogViewer.Presenter(
				new PresentationModel(model, pendingUpdateFlag), view.MessagesView, navHandler);
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.Presenter.PreferredDblClickAction.SelectWord;
			this.UpdateRawViewButton();
			this.UpdateColoringControls();
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();

			model.Bookmarks.OnBookmarksChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			model.DisplayFilters.OnPropertiesChanged += (s, e) =>
			{
				if (e.ChangeAffectsFilterResult)
					pendingUpdateFlag.Invalidate();
			};
			model.DisplayFilters.OnFiltersListChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			model.DisplayFilters.OnFilteringEnabledChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			model.HighlightFilters.OnPropertiesChanged += (s, e) =>
			{
				if (e.ChangeAffectsFilterResult)
					pendingUpdateFlag.Invalidate();
			};
			model.HighlightFilters.OnFiltersListChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			model.HighlightFilters.OnFilteringEnabledChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};
			model.OnMessagesChanged += (s, e) =>
			{
				pendingUpdateFlag.Invalidate();
			};

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && !model.SourcesManager.AtLeastOneSourceIsBeingLoaded() && pendingUpdateFlag.Validate())
				{
					UpdateView();
					model.SourcesManager.SetCurrentViewPositionIfNeeded();
				}
			};

			this.view.SetPresenter(this);
		}

		bool IPresenter.RawViewAllowed
		{
			get { return messagesPresenter.RawViewAllowed; }
			set { messagesPresenter.RawViewAllowed = value; }
		}

		void IPresenter.ToggleBookmark()
		{
			var msg = messagesPresenter.Selection.Message;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		void IPresenter.ToggleRawView()
		{
			messagesPresenter.ShowRawMessages = messagesPresenter.RawViewAllowed && !messagesPresenter.ShowRawMessages;
		}

		void IPresenter.ColoringButtonClicked(LogViewer.ColoringMode mode)
		{
			messagesPresenter.Coloring = mode;
			UpdateColoringControls();
		}

		LogViewer.Presenter IPresenter.LogViewerPresenter
		{
			get { return messagesPresenter; }
		}

		void IPresenter.Focus()
		{
			view.Focus();
		}

		void UpdateRawViewButton()
		{
			view.SetRawViewButtonState(messagesPresenter.RawViewAllowed, messagesPresenter.ShowRawMessages);
		}

		void UpdateView()
		{
			messagesPresenter.UpdateView();
		}

		void UpdateColoringControls()
		{
			var coloring = messagesPresenter.Coloring;
			view.SetColoringButtonsState(
				coloring == LogViewer.ColoringMode.None,
				coloring == LogViewer.ColoringMode.Sources,
				coloring == LogViewer.ColoringMode.Threads
			);
		}
	};
};