using LogJoint.Analytics;
using LogJoint.Extensibility;
using LogJoint.Postprocessing;
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
					return null;
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
			//var stateInspectorFormImp = new LogJoint.UI.Postprocessing.StateInspector.StateInspectorForm();
			//app.View.RegisterToolForm(stateInspectorFormImp);
			var viewObjects = CreateStateInspectorViewObjects();
			stateInspectorForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			stateInspectorModel = new LogJoint.Postprocessing.StateInspector.StateInspectorVisualizerModel(
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.ModelThreadSynchronization,
				app.Model.Postprocessing.ShortNames
			);
			stateInspectorPresenter = new LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.StateInspectorPresenter(
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
			var timelineViewObjects = CreateTimelineViewObjects();
			//var timelineFormImp = new LogJoint.UI.Postprocessing.TimelineVisualizer.TimelineForm();
			//app.View.RegisterToolForm(timelineFormImp);
			//view = timelineFormImp.TimelineVisualizerView;
			timelineForm = timelineViewObjects.Item1;
			var view = timelineViewObjects.Item2;
			timelineModel = new LogJoint.Postprocessing.Timeline.TimelineVisualizerModel(
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.Postprocessing.ShortNames,
				app.Model.Postprocessing.LogSourceNamesProvider
			);
			timelinePresenter = new LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.TimelineVisualizerPresenter(
				timelineModel,
				view,
				stateInspectorPresenter,
				new LogJoint.UI.Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.PostprocessorsManager, app.Model.SourcesManager),
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
			//var sequenceDiagramFormImp = new LogJoint.UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramForm();
			//app.View.RegisterToolForm(sequenceDiagramFormImp);
			// view = sequenceDiagramFormImp.SequenceDiagramVisualizerView;
			sequenceDiagramForm = viewObjects.Item1;
			var view = viewObjects.Item2;
			sequenceDiagramModel = new LogJoint.Postprocessing.SequenceDiagram.SequenceDiagramVisualizerModel(
				app.Model.Postprocessing.PostprocessorsManager,
				app.Model.SourcesManager,
				app.Model.Postprocessing.ShortNames,
				app.Model.Postprocessing.LogSourceNamesProvider
			);
			sequenceDiagramPresenter = new LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenter(
				sequenceDiagramModel,
				view,
				stateInspectorPresenter,
				new LogJoint.UI.Presenters.Postprocessing.Common.PresentationObjectsFactory(app.Model.Postprocessing.PostprocessorsManager, app.Model.SourcesManager),
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
			// todo: have good portable implemenetation of time series view
		}
	}
}
