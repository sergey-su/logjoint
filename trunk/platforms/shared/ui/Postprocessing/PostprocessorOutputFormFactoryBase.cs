using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing
{
	public abstract class PostprocessorOutputFormFactoryBase : IPostprocessorOutputFormFactory
	{
		protected LogJoint.IApplication app;
		private ILogSourceNamesProvider logSourceNamesProvider;
		private IUserNamesProvider shortNames;
		private Presenters.SourcesManager.IPresenter sourcesManagerPresenter;
		private Presenters.LoadedMessages.IPresenter loadedMessagesPresenter;
		private Presenters.IClipboardAccess clipboardAccess;
		private Presenters.IPresentersFacade presentersFacade;
		private Presenters.IAlertPopup alerts;

		LogJoint.Postprocessing.StateInspector.IStateInspectorVisualizerModel stateInspectorModel;
		UI.Presenters.Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorPresenter;
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm stateInspectorForm;

		LogJoint.Postprocessing.Timeline.ITimelineVisualizerModel timelineModel;
		UI.Presenters.Postprocessing.TimelineVisualizer.IPresenter timelinePresenter;
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm timelineForm;

		LogJoint.Postprocessing.SequenceDiagram.ISequenceDiagramVisualizerModel sequenceDiagramModel;
		LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.IPresenter sequenceDiagramPresenter;
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm sequenceDiagramForm;

		LogJoint.Postprocessing.TimeSeries.ITimeSeriesVisualizerModel timeSeriesModel;
		LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IPresenter timeSeriesPresenter;
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm timeSeriesForm;

		readonly Dictionary<ViewControlId, Func<IPostprocessorOutputForm>> customFactories = new Dictionary<ViewControlId, Func<IPostprocessorOutputForm>>();

		public PostprocessorOutputFormFactoryBase (
		)
		{
		}

		public void Init (
			LogJoint.IApplication app,
			ILogSourceNamesProvider logSourceNamesProvider,
			IUserNamesProvider shortNames,
			Presenters.SourcesManager.IPresenter sourcesManagerPresenter,
			Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.IClipboardAccess clipboardAccess,
			Presenters.IPresentersFacade presentersFacade,
			Presenters.IAlertPopup alerts
		)
		{
			this.app = app;
			this.logSourceNamesProvider = logSourceNamesProvider;
			this.shortNames = shortNames;
			this.sourcesManagerPresenter = sourcesManagerPresenter;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.clipboardAccess = clipboardAccess;
			this.presentersFacade = presentersFacade;
			this.alerts = alerts;
		}
		
		protected abstract Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.StateInspectorVisualizer.IView> CreateStateInspectorViewObjects();
		protected abstract Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimelineVisualizer.IView> CreateTimelineViewObjects();
		protected abstract Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.SequenceDiagramVisualizer.IView> CreateSequenceDiagramViewObjects();
		protected abstract Tuple<IPostprocessorOutputForm, Presenters.Postprocessing.TimeSeriesVisualizer.IView> CreateTimeSeriesViewObjects();

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
			var viewObjects = CreateStateInspectorViewObjects();
			stateInspectorForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			stateInspectorModel = new LogJoint.Postprocessing.StateInspector.StateInspectorVisualizerModel(
				app.Model.Postprocessing.Manager,
				app.Model.SourcesManager,
				app.Model.ModelThreadSynchronization,
				shortNames
			);
			stateInspectorPresenter = new Presenters.Postprocessing.StateInspectorVisualizer.StateInspectorPresenter(
				view,
				stateInspectorModel,
				shortNames,
				app.Model.SourcesManager,
				loadedMessagesPresenter,
				app.Model.Bookmarks,
				app.Model.Threads,
				presentersFacade,
				clipboardAccess,
				sourcesManagerPresenter,
				app.Presentation.Theme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.StateInspector, stateInspectorForm, stateInspectorPresenter));
		}

		void EnsureTimelineInitialized()
		{
			if (timelineForm != null)
				return;
			EnsureStateInspectorInitialized();
			var viewObjects = CreateTimelineViewObjects();
			timelineForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			timelineModel = new LogJoint.Postprocessing.Timeline.TimelineVisualizerModel(
				app.Model.Postprocessing.Manager,
				app.Model.SourcesManager,
				shortNames,
				logSourceNamesProvider
			);
			timelinePresenter = new Presenters.Postprocessing.TimelineVisualizer.TimelineVisualizerPresenter(
				timelineModel,
				view,
				stateInspectorPresenter,
				new Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.Manager, app.Model.SourcesManager, app.Model.ChangeNotification, alerts),
				loadedMessagesPresenter,
				app.Model.Bookmarks,
				app.Model.StorageManager,
				presentersFacade,
				shortNames,
				app.Model.ChangeNotification,
				app.Presentation.Theme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.Timeline, timelineForm, timelinePresenter));
		}

		void EnsureSequenceDiagramInitialized()
		{
			if (sequenceDiagramForm != null)
				return;

			EnsureStateInspectorInitialized();

			var viewObjects = CreateSequenceDiagramViewObjects();
			sequenceDiagramForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			sequenceDiagramModel = new LogJoint.Postprocessing.SequenceDiagram.SequenceDiagramVisualizerModel(
				app.Model.Postprocessing.Manager,
				app.Model.SourcesManager,
				shortNames,
				logSourceNamesProvider,
				app.Model.ChangeNotification
			);
			sequenceDiagramPresenter = new Presenters.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenter(
				sequenceDiagramModel,
				view,
				stateInspectorPresenter,
				new Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.Manager, app.Model.SourcesManager, app.Model.ChangeNotification, alerts),
				loadedMessagesPresenter,
				app.Model.Bookmarks,
				app.Model.StorageManager,
				presentersFacade,
				shortNames,
				app.Model.ChangeNotification,
				app.Presentation.Theme
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.Sequence, sequenceDiagramForm, sequenceDiagramPresenter));
		}

		void EnsureTimeSeriesInitialized()
		{
			if (timeSeriesForm != null)
				return;

			var viewObjects = CreateTimeSeriesViewObjects();
			timeSeriesForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			timeSeriesModel = new LogJoint.Postprocessing.TimeSeries.TimelineVisualizerModel(
				app.Model.Postprocessing.Manager,
				app.Model.SourcesManager,
				shortNames,
				logSourceNamesProvider
			);
			timeSeriesPresenter = new Presenters.Postprocessing.TimeSeriesVisualizer.TimeSeriesVisualizerPresenter(
				timeSeriesModel,
				view,
				new Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.Manager, app.Model.SourcesManager, app.Model.ChangeNotification, alerts),
				loadedMessagesPresenter.LogViewerPresenter,
				app.Model.Bookmarks,
				presentersFacade,
				app.Model.ChangeNotification
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.TimeSeries, timeSeriesForm, timeSeriesPresenter));
		}
	}
}
