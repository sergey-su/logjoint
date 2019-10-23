using System;
using LogJoint.Postprocessing;

namespace LogJoint.UI.Presenters.Postprocessing
{
	public interface IFactory
	{
		StateInspectorVisualizer.IPresenterInternal GetStateInspectorVisualizer(bool create);
		event EventHandler StateInspectorCreated;
		TimelineVisualizer.IPresenter GetTimelineVisualizer(bool create);
		SequenceDiagramVisualizer.IPresenter GetSequenceDiagramVisualizer(bool create);
		TimeSeriesVisualizer.IPresenter GetTimeSeriesVisualizer(bool create);
	};

	public class Factory: IFactory
	{
		public interface IViewsFactory
		{
			StateInspectorVisualizer.IView CreateStateInspectorView();
			TimelineVisualizer.IView CreateTimelineView();
			SequenceDiagramVisualizer.IView CreateSequenceDiagramView();
			TimeSeriesVisualizer.IView CreateTimeSeriesView();
		};

		private readonly Lazy<StateInspectorVisualizer.IPresenterInternal> stateInspectorVisualizer;
		private readonly Lazy<TimelineVisualizer.IPresenter> timelineVisualizer;
		private readonly Lazy<SequenceDiagramVisualizer.IPresenter> sequenceDiagramVisualizer;
		private readonly Lazy<TimeSeriesVisualizer.IPresenter> timeSeriesVisualizer;

		StateInspectorVisualizer.IPresenterInternal IFactory.GetStateInspectorVisualizer(bool create) => Get(stateInspectorVisualizer, create, StateInspectorCreated);
		public event EventHandler StateInspectorCreated;
		TimelineVisualizer.IPresenter IFactory.GetTimelineVisualizer(bool create) => Get(timelineVisualizer, create, null);
		SequenceDiagramVisualizer.IPresenter IFactory.GetSequenceDiagramVisualizer(bool create) => Get(sequenceDiagramVisualizer, create, null);
		TimeSeriesVisualizer.IPresenter IFactory.GetTimeSeriesVisualizer(bool create) => Get(timeSeriesVisualizer, create, null);

		public Factory(
			IViewsFactory postprocessingViewsFactory,
			IManagerInternal postprocessorsManager,
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
			IColorTheme colorTheme,
			Drawing.IMatrixFactory matrixFactory
		)
		{
			stateInspectorVisualizer = new Lazy<StateInspectorVisualizer.IPresenterInternal>(() =>
			{
				var view = postprocessingViewsFactory.CreateStateInspectorView();
				var model = new LogJoint.Postprocessing.StateInspector.StateInspectorVisualizerModel(
					postprocessorsManager,
					logSourcesManager,
					synchronizationContext,
					shortNames
				);
				return new StateInspectorVisualizer.StateInspectorPresenter(
					view,
					model,
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
			});

			timelineVisualizer = new Lazy<TimelineVisualizer.IPresenter>(() =>
			{
				var view = postprocessingViewsFactory.CreateTimelineView();
				var model = new LogJoint.Postprocessing.Timeline.TimelineVisualizerModel(
					postprocessorsManager,
					logSourcesManager,
					shortNames,
					logSourceNamesProvider
				);
				return new TimelineVisualizer.TimelineVisualizerPresenter(
					model,
					view,
					stateInspectorVisualizer.Value,
					new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
					loadedMessagesPresenter,
					bookmarks,
					storageManager,
					presentersFacade,
					shortNames,
					changeNotification,
					colorTheme
				);
			});

			sequenceDiagramVisualizer = new Lazy<SequenceDiagramVisualizer.IPresenter>(() =>
			{
				var view = postprocessingViewsFactory.CreateSequenceDiagramView();
				var model = new LogJoint.Postprocessing.SequenceDiagram.SequenceDiagramVisualizerModel(
					postprocessorsManager,
					logSourcesManager,
					shortNames,
					logSourceNamesProvider,
					changeNotification
				);
				return new SequenceDiagramVisualizer.SequenceDiagramVisualizerPresenter(
					model,
					view,
					stateInspectorVisualizer.Value,
					new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
					loadedMessagesPresenter,
					bookmarks,
					storageManager,
					presentersFacade,
					shortNames,
					changeNotification,
					colorTheme,
					matrixFactory
				);
			});

			timeSeriesVisualizer = new Lazy<TimeSeriesVisualizer.IPresenter>(() =>
			{
				var view = postprocessingViewsFactory.CreateTimeSeriesView();
				var model = new LogJoint.Postprocessing.TimeSeries.TimelineVisualizerModel(
					postprocessorsManager,
					logSourcesManager,
					shortNames,
					logSourceNamesProvider
				);
				return new TimeSeriesVisualizer.TimeSeriesVisualizerPresenter(
					model,
					view,
					new Common.PresentationObjectsFactory(postprocessorsManager, logSourcesManager, changeNotification, alerts),
					loadedMessagesPresenter.LogViewerPresenter,
					bookmarks,
					presentersFacade,
					changeNotification
				);
			});
		}

		static T Get<T>(Lazy<T> lazy, bool create, EventHandler created) where T: class
		{
			if (lazy.IsValueCreated)
				return lazy.Value;
			if (!create)
				return null;
			var value = lazy.Value;
			created?.Invoke(value, EventArgs.Empty);
			return value;
		}
	}
}
