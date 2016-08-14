using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IModel model;
		readonly IView view;
		readonly LogViewer.IPresenter messagesPresenter;
		readonly LazyUpdateFlag rawViewUpdateFlag = new LazyUpdateFlag();
		bool automaticRawView = true;

		public Presenter(
			IModel model,
			IView view,
			IHeartBeatTimer heartbeat,
			LogViewer.IPresenterFactory logViewerPresenterFactory
		)
		{
			this.model = model;
			this.view = view;
			this.messagesPresenter =  logViewerPresenterFactory.Create(
				new PresentationModel(model),
				view.MessagesView,
				createIsolatedPresenter: false
			);
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.SelectWord;
			this.UpdateRawViewButton();
			this.UpdateColoringControls();
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();
			this.messagesPresenter.NavigationIsInProgressChanged += (s, e) => 
				{ view.SetNavigationProgressIndicatorVisibility(messagesPresenter.NavigationIsInProgress); };

			heartbeat.OnTimer += (sender, args) =>
			{
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
			var msg = messagesPresenter.FocusedMessage;
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

		async Task<Dictionary<ILogSource, long>> IPresenter.GetCurrentLogPositions(CancellationToken cancellation)
		{
			var viewerPositions = await messagesPresenter.GetCurrentPositions(cancellation);
			if (viewerPositions == null)
				return null;
			return viewerPositions
				.Select(p => new { src = PresentationModel.MessagesSourceToLogSource(p.Key), pos = p.Value })
				.Where(p => p.src != null)
				.ToDictionary(p => p.src, p => p.pos);
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