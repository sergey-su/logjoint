using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IModel model;
		readonly IView view;
		readonly LogViewer.IPresenter messagesPresenter;
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
		readonly LazyUpdateFlag rawViewUpdateFlag = new LazyUpdateFlag();
		bool automaticRawView = true;

		public Presenter(
			IModel model,
			IView view,
			IPresentersFacade navHandler,
			IHeartBeatTimer heartbeat,
			IClipboardAccess clipboard
		)
		{
			this.model = model;
			this.view = view;
			this.messagesPresenter = new Presenters.LogViewer.Presenter(
				new PresentationModel(model, pendingUpdateFlag), 
				view.MessagesView, 
				navHandler,
				clipboard);
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.SelectWord;
			this.UpdateRawViewButton();
			this.UpdateColoringControls();
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();

			model.Bookmarks.OnBookmarksChanged += (s, e) =>
			{
				messagesPresenter.InvalidateView();
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
				if (args.IsNormalUpdate && rawViewUpdateFlag.Validate())
				{
					UpdateRawViewAvailability();
					UpdateRawViewMode();
					UpdateRawViewButton();
				}
			};
			model.SourcesManager.OnLogSourceRemoved += (sender, evt) =>
			{
				if (model.SourcesManager.Items.Count(s => !s.IsDisposed) == 0)
					automaticRawView = true; // reset automatic mode when last source is gone
				rawViewUpdateFlag.Invalidate();
			};
			model.SourcesManager.OnLogSourceAdded += (sender, evt) =>
			{
				rawViewUpdateFlag.Invalidate();
			};
			model.SourcesManager.OnLogSourceVisiblityChanged += (sender, evt) =>
			{
				rawViewUpdateFlag.Invalidate();
			};


			this.view.SetEventsHandler(this);
		}

		public event EventHandler OnResizingStarted;
		public event EventHandler<ResizingEventArgs> OnResizing;
		public event EventHandler OnResizingFinished;

		void IViewEvents.OnToggleBookmark()
		{
			var msg = messagesPresenter.Selection.Message;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		void IViewEvents.OnToggleRawView()
		{
			messagesPresenter.ShowRawMessages = !messagesPresenter.ShowRawMessages;
			automaticRawView = false; // when mode is manually changed -> stop automatic selection of raw view
		}

		void IViewEvents.OnColoringButtonClicked(Settings.Appearance.ColoringMode mode)
		{
			messagesPresenter.Coloring = mode;
			UpdateColoringControls();
		}

		LogViewer.IPresenter IPresenter.LogViewerPresenter
		{
			get { return messagesPresenter; }
		}

		void IPresenter.Focus()
		{
			view.Focus();
		}

		void IViewEvents.OnResizingFinished()
		{
			if (OnResizingFinished != null)
				OnResizingFinished(this, EventArgs.Empty);
		}

		void IViewEvents.OnResizing(int delta)
		{
			if (OnResizing != null)
				OnResizing(this, new ResizingEventArgs() { Delta = delta });
		}

		void IViewEvents.OnResizingStarted()
		{
			if (OnResizingStarted != null)
				OnResizingStarted(this, EventArgs.Empty);
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
				coloring == Settings.Appearance.ColoringMode.None,
				coloring == Settings.Appearance.ColoringMode.Sources,
				coloring == Settings.Appearance.ColoringMode.Threads
			);
		}

		IEnumerable<ILogSource> EnumVisibleSources()
		{
			return model.SourcesManager.Items.Where(s => !s.IsDisposed && s.Visible);
		}

		void UpdateRawViewAvailability()
		{
			bool rawViewAllowed = EnumVisibleSources().Any(s => s.Provider.Factory.ViewOptions.RawViewAllowed);
			messagesPresenter.RawViewAllowed = rawViewAllowed;
		}

		void UpdateRawViewMode()
		{
			if (automaticRawView)
			{
				bool allWantRawView = EnumVisibleSources().All(s => s.Provider.Factory.ViewOptions.PreferredView == PreferredViewMode.Raw);
				messagesPresenter.ShowRawMessages = allWantRawView;
			}
		}
	};
};