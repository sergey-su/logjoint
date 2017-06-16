using LogJoint.Extensibility;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing
{
	public abstract class PostprocessorOutputFormFactoryBase : IPostprocessorOutputFormFactory
	{
		protected IApplication app;

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

		public void Init(IApplication app)
		{
			this.app = app;
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
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.ModelThreadSynchronization,
				app.Model.Postprocessing.ShortNames
			);
			stateInspectorPresenter = new Presenters.Postprocessing.StateInspectorVisualizer.StateInspectorPresenter(
				view,
				stateInspectorModel,
				app.Model.Postprocessing.ShortNames,
				app.Model.SourcesManager,
				app.Presentation.LoadedMessages,
				app.Model.Bookmarks,
				app.Model.Threads,
				app.Presentation.Facade,
				app.Presentation.ClipboardAccess,
				app.Presentation.SourcesManager
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
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.Postprocessing.ShortNames,
				app.Model.Postprocessing.LogSourceNamesProvider
			);
			timelinePresenter = new Presenters.Postprocessing.TimelineVisualizer.TimelineVisualizerPresenter(
				timelineModel,
				view,
				stateInspectorPresenter,
				new Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.PostprocessorsManager, app.Model.SourcesManager),
				app.Presentation.LoadedMessages,
				app.Model.Bookmarks,
				app.Model.StorageManager,
				app.Presentation.Facade,
				app.Model.Postprocessing.ShortNames
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
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.Postprocessing.ShortNames,
				app.Model.Postprocessing.LogSourceNamesProvider
			);
			sequenceDiagramPresenter = new Presenters.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenter(
				sequenceDiagramModel,
				view,
				stateInspectorPresenter,
				new Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.PostprocessorsManager, app.Model.SourcesManager),
				app.Presentation.LoadedMessages,
				app.Model.Bookmarks,
				app.Model.StorageManager,
				app.Presentation.Facade,
				app.Model.Postprocessing.ShortNames
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
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.Postprocessing.ShortNames,
				app.Model.Postprocessing.LogSourceNamesProvider
			);
			timeSeriesPresenter = new Presenters.Postprocessing.TimeSeriesVisualizer.TimeSeriesVisualizerPresenter(
				timeSeriesModel,
				view,
				app.Presentation.LoadedMessages.LogViewerPresenter,
				app.Model.Bookmarks,
				app.Presentation.Facade
			);
			FormCreated?.Invoke(this, new PostprocessorOutputFormCreatedEventArgs(ViewControlId.TimeSeries, timeSeriesForm, timeSeriesPresenter));
		}
	}
}
