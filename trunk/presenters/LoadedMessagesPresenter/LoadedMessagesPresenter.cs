using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;
using static LogJoint.Settings.Appearance;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly ILogSourcesManager logSources;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly LogViewer.IPresenter messagesPresenter;
		readonly IChangeNotification changeNotification;
		readonly Func<ViewState> viewState;
		readonly (ColoringMode Mode, string Text, string Tooltip)[] coloringOptions = {
			(ColoringMode.None, "None", "All log messages have same background"),
			(ColoringMode.Threads, "Threads", "Messages of different threads have different color"),
			(ColoringMode.Sources, "Log sources", "All messages of the same log source have same color")
		};

		public Presenter(
			ILogSourcesManager logSources,
			IBookmarks bookmarks,
			IView view,
			IHeartBeatTimer heartbeat,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			IChangeNotification changeNotification,
			ISynchronizationContext synchronizationContext
		)
		{
			this.logSources = logSources;
			this.bookmarks = bookmarks;
			this.view = view;
			this.changeNotification = changeNotification;
			this.messagesPresenter =  logViewerPresenterFactory.CreateLoadedMessagesPresenter(view.MessagesView);
			this.messagesPresenter.DblClickAction = LogViewer.PreferredDblClickAction.SelectWord;

			var viewColoringOptions = coloringOptions.Select(i => (i.Text, i.Tooltip)).ToArray().AsReadOnly();

			this.viewState = Selectors.Create(
				() => (messagesPresenter.RawViewAllowed, messagesPresenter.ShowRawMessages),
				() => messagesPresenter.ViewTailMode,
				() => logSources.VisibleItems,
				() => messagesPresenter.Coloring,
				() => messagesPresenter.NavigationIsInProgress,
				(raw, viewTailMode, sources, coloring, navigation) => new ViewState
				{
					ToggleBookmark = (Visible: sources.Count > 0, Tooltip: "Log coloring"),
					RawViewButton = (Visible: raw.RawViewAllowed, Checked: raw.ShowRawMessages, Tooltip: "Toggle raw log view"),
					ViewTailButton = (Visible: sources.Count > 0, Checked: viewTailMode, Tooltip: viewTailMode ? "Stop autoscrolling to log end" : "Autoscroll to log end"),
					Coloring = (Visible: sources.Count > 0, Options: viewColoringOptions, Selected: coloringOptions.IndexOf(option => option.Mode == coloring).GetValueOrDefault(0)),
					NavigationProgressIndicator = (Visible: navigation, Tooltip: "Loading..."),
				}
			);

			this.view.SetViewModel(this);
		}

		public event EventHandler OnResizingStarted;
		public event EventHandler<ResizingEventArgs> OnResizing;
		public event EventHandler OnResizingFinished;

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		ViewState IViewModel.ViewState => viewState();

		void IViewModel.OnToggleBookmark()
		{
			bookmarks.ToggleBookmark(messagesPresenter.FocusedMessageBookmark);
		}

		void IViewModel.OnToggleRawView()
		{
			messagesPresenter.ShowRawMessages = !messagesPresenter.ShowRawMessages;
		}

		void IViewModel.OnToggleViewTail ()
		{
			messagesPresenter.ViewTailMode = !messagesPresenter.ViewTailMode;
		}

		void IViewModel.OnColoringButtonClicked(int modeIndex)
		{
			messagesPresenter.Coloring = coloringOptions[modeIndex].Mode;
		}

		LogViewer.IPresenter IPresenter.LogViewerPresenter
		{
			get { return messagesPresenter; }
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

		void IViewModel.OnResizingFinished()
		{
			OnResizingFinished?.Invoke (this, EventArgs.Empty);
		}

		void IViewModel.OnResizing(int delta)
		{
			OnResizing?.Invoke (this, new ResizingEventArgs () { Delta = delta });
		}

		void IViewModel.OnResizingStarted()
		{
			OnResizingStarted?.Invoke (this, EventArgs.Empty);
		}
	};
};