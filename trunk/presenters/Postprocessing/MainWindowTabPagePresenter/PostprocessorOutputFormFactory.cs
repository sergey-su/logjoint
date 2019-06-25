using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing
{
	public class Factory : IPostprocessorOutputFormFactory
	{
		private readonly IViewsFactory postprocessingViewsFactory;
		private readonly ILogSourcesManager logSourcesManager;
		private readonly IPostprocessorsManager postprocessorsManager;
		private readonly ISynchronizationContext synchronizationContext;
		private readonly IChangeNotification changeNotification;
		private readonly ILogSourceNamesProvider logSourceNamesProvider;
		private readonly IBookmarks bookmarks;
		private readonly IModelThreads threads;
		private readonly Persistence.IStorageManager storageManager;
		private readonly IUserNamesProvider shortNames;
		private readonly SourcesManager.IPresenter sourcesManagerPresenter;
		private readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		private readonly IClipboardAccess clipboardAccess;
		private readonly IPresentersFacade presentersFacade;
		private readonly IAlertPopup alerts;
		private readonly IColorTheme colorTheme;

		LogJoint.Postprocessing.StateInspector.IStateInspectorVisualizerModel stateInspectorModel;
		StateInspectorVisualizer.IPresenter stateInspectorPresenter;
		IPostprocessorOutputForm stateInspectorForm;

		LogJoint.Postprocessing.Timeline.ITimelineVisualizerModel timelineModel;
		TimelineVisualizer.IPresenter timelinePresenter;
		IPostprocessorOutputForm timelineForm;

		LogJoint.Postprocessing.SequenceDiagram.ISequenceDiagramVisualizerModel sequenceDiagramModel;
		SequenceDiagramVisualizer.IPresenter sequenceDiagramPresenter;
		IPostprocessorOutputForm sequenceDiagramForm;

		LogJoint.Postprocessing.TimeSeries.ITimeSeriesVisualizerModel timeSeriesModel;
		TimeSeriesVisualizer.IPresenter timeSeriesPresenter;
		IPostprocessorOutputForm timeSeriesForm;

		readonly Dictionary<ViewControlId, Func<IPostprocessorOutputForm>> customFactories = new Dictionary<ViewControlId, Func<IPostprocessorOutputForm>>();

		public interface IViewsFactory
		{
			(IPostprocessorOutputForm, StateInspectorVisualizer.IView) CreateStateInspectorViewObjects();
			(IPostprocessorOutputForm, TimelineVisualizer.IView) CreateTimelineViewObjects();
			(IPostprocessorOutputForm, SequenceDiagramVisualizer.IView) CreateSequenceDiagramViewObjects();
			(IPostprocessorOutputForm, TimeSeriesVisualizer.IView) CreateTimeSeriesViewObjects();
		};

		public Factory(
			IViewsFactory postprocessingViewsFactory,
			IPostprocessorsManager postprocessorsManager,
			ILogSourcesManager logSourcesManager,
			ISynchronizationContext synchronizationContext,
			IChangeNotification changeNotification,
			IBookmarks bookmarks,
			IModelThreads threads,
			Persistence.IStorageManager storageManager,
			ILogSourceNamesProvider logSourceNamesProvider,
			IUserNamesProvider shortNames,
			SourcesManager.IPresenter sourcesManagerPresenter,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IClipboardAccess clipboardAccess,
			IPresentersFacade presentersFacade,
			IAlertPopup alerts,
			IColorTheme colorTheme
		)
		{
			this.postprocessingViewsFactory = postprocessingViewsFactory;
			this.postprocessorsManager = postprocessorsManager;
			this.logSourcesManager = logSourcesManager;
			this.synchronizationContext = synchronizationContext;
			this.changeNotification = changeNotification;
			this.bookmarks = bookmarks;
			this.threads = threads;
			this.storageManager = storageManager;
			this.logSourceNamesProvider = logSourceNamesProvider;
			this.shortNames = shortNames;
			this.sourcesManagerPresenter = sourcesManagerPresenter;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.clipboardAccess = clipboardAccess;
			this.presentersFacade = presentersFacade;
			this.alerts = alerts;
			this.colorTheme = colorTheme;
		}
		
		IPostprocessorOutputForm IPostprocessorOutputFormFactory.GetPostprocessorOutputForm(ViewControlId id)
		{
			Func<IPostprocessorOutputForm> facMethod;
			if (customFactories.TryGetValue(id, out facMethod) && facMethod != null)
				return facMethod();
			switch (id)
			{
				case ViewControlId.StateInspector:
					EnsureStateInspectorInitialized();
					return stateInspectorForm;
				case ViewControlId.Timeline:
					EnsureTimelineInitialized();
					return timelineForm;
				case ViewControlId.Sequence:
					EnsureSequenceDiagramInitialized();
					return sequenceDiagramForm;
				case ViewControlId.TimeSeries:
					EnsureTimeSeriesInitialized();
					return timeSeriesForm;
				default:
					return null;
			}
		}

		void IPostprocessorOutputFormFactory.OverrideFormFactory(ViewControlId id, Func<IPostprocessorOutputForm> factory)
		{
			customFactories[id] = factory;
		}

		public event EventHandler<PostprocessorOutputFormCreatedEventArgs> FormCreated;

		void EnsureStateInspectorInitialized()
		{
			if (stateInspectorForm != null)
				return;
			var viewObjects = postprocessingViewsFactory.CreateStateInspectorViewObjects();
			stateInspectorForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			stateInspectorModel = new LogJoint.Postprocessing.StateInspector.StateInspectorVisualizerModel(
				postprocessorsManager,
				logSourcesManager,
				synchronizationContext,
				shortNames
			);
			stateInspectorPresenter = new StateInspectorVisualizer.StateInspectorPresenter(
				view,
				stateInspectorModel,
				shortNames,
				logSourcesManager,
				loadedMessagesPresenter,
				bookmarks,
				threads,
				presentersFacade,
				clipboardAccess,
				sourcesManagerPresenter,
				colorTheme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.StateInspector, stateInspectorForm, stateInspectorPresenter));
		}

		void EnsureTimelineInitialized()
		{
			if (timelineForm != null)
				return;
			EnsureStateInspectorInitialized();
			var viewObjects = postprocessingViewsFactory.CreateTimelineViewObjects();
			timelineForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			timelineModel = new LogJoint.Postprocessing.Timeline.TimelineVisualizerModel(
				postprocessorsManager,
				logSourcesManager,
				shortNames,
				logSourceNamesProvider
			);
			timelinePresenter = new TimelineVisualizer.TimelineVisualizerPresenter(
				timelineModel,
				view,
				stateInspectorPresenter,
				new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
				loadedMessagesPresenter,
				bookmarks,
				storageManager,
				presentersFacade,
				shortNames,
				changeNotification,
				colorTheme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.Timeline, timelineForm, timelinePresenter));
		}

		void EnsureSequenceDiagramInitialized()
		{
			if (sequenceDiagramForm != null)
				return;

			EnsureStateInspectorInitialized();

			var viewObjects = postprocessingViewsFactory.CreateSequenceDiagramViewObjects();
			sequenceDiagramForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			sequenceDiagramModel = new LogJoint.Postprocessing.SequenceDiagram.SequenceDiagramVisualizerModel(
				postprocessorsManager,
				logSourcesManager,
				shortNames,
				logSourceNamesProvider,
				changeNotification
			);
			sequenceDiagramPresenter = new SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenter(
				sequenceDiagramModel,
				view,
				stateInspectorPresenter,
				new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
				loadedMessagesPresenter,
				bookmarks,
				storageManager,
				presentersFacade,
				shortNames,
				changeNotification,
				colorTheme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.Sequence, sequenceDiagramForm, sequenceDiagramPresenter));
		}

		void EnsureTimeSeriesInitialized()
		{
			if (timeSeriesForm != null)
				return;

			var viewObjects = postprocessingViewsFactory.CreateTimeSeriesViewObjects();
			timeSeriesForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			timeSeriesModel = new LogJoint.Postprocessing.TimeSeries.TimelineVisualizerModel(
				postprocessorsManager,
				logSourcesManager,
				shortNames,
				logSourceNamesProvider
			);
			timeSeriesPresenter = new TimeSeriesVisualizer.TimeSeriesVisualizerPresenter(
				timeSeriesModel,
				view,
				new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
				loadedMessagesPresenter.LogViewerPresenter,
				bookmarks,
				presentersFacade,
				changeNotification
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.TimeSeries, timeSeriesForm, timeSeriesPresenter));
		}
	}
}
