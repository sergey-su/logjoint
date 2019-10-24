using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using LogJoint.Postprocessing.Correlation;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public class Presenter: IPresenter, IViewModel
	{
		readonly IView view;
		readonly IManagerInternal postprocessorsManager;
		readonly ICorrelationManager correlationManager;
		readonly IFactory presentersFactory;
		readonly Dictionary<ViewControlId, IViewControlHandler> viewControlHandlers = new Dictionary<ViewControlId, IViewControlHandler>();
		readonly ITempFilesManager tempFiles;
		readonly IShellOpen shellOpen;
		readonly IChainedChangeNotification changeNotification;
		Func<IImmutableDictionary<ViewControlId, ControlData>> getControlsData;

		public Presenter(
			IView view,
			IManagerInternal postprocessorsManager,
			ICorrelationManager correlationManager,
			IFactory presentersFactory,
			ITempFilesManager tempFiles,
			IShellOpen shellOpen,
			NewLogSourceDialog.IPresenter newLogSourceDialog,
			IChangeNotification changeNotification,
			MainForm.IPresenter mainFormPresenter
		)
		{
			this.view = view;
			this.postprocessorsManager = postprocessorsManager;
			this.correlationManager = correlationManager;
			this.presentersFactory = presentersFactory;
			this.tempFiles = tempFiles;
			this.shellOpen = shellOpen;
			this.changeNotification = changeNotification.CreateChainedChangeNotification(false);


			InitAndAddProstprocessorHandler(ViewControlId.StateInspector, PostprocessorKind.StateInspector);
			InitAndAddProstprocessorHandler(ViewControlId.Timeline, PostprocessorKind.Timeline);
			InitAndAddProstprocessorHandler(ViewControlId.Sequence, PostprocessorKind.SequenceDiagram);
			InitAndAddProstprocessorHandler(ViewControlId.Correlate, PostprocessorKind.Correlator);
			InitAndAddProstprocessorHandler(ViewControlId.TimeSeries, PostprocessorKind.TimeSeries);
			viewControlHandlers.Add(ViewControlId.LogsCollectionControl1, new GenericLogsOpenerControlHandler(newLogSourceDialog));
			viewControlHandlers.Add(ViewControlId.AllPostprocessors, new AllPostprocessorsControlHandler(postprocessorsManager));

			this.getControlsData = Selectors.Create(
				() => (
					viewControlHandlers[ViewControlId.StateInspector].GetCurrentData(),
					viewControlHandlers[ViewControlId.Timeline].GetCurrentData(),
					viewControlHandlers[ViewControlId.Sequence].GetCurrentData(),
					viewControlHandlers[ViewControlId.Correlate].GetCurrentData(),
					viewControlHandlers[ViewControlId.TimeSeries].GetCurrentData(),
					viewControlHandlers[ViewControlId.LogsCollectionControl1].GetCurrentData(),
					viewControlHandlers[ViewControlId.AllPostprocessors].GetCurrentData()
				), _ => {
					return ImmutableDictionary.CreateRange(
						viewControlHandlers.Select(h => new KeyValuePair<ViewControlId, ControlData>(h.Key, h.Value.GetCurrentData()))
					);
				}
			);

			this.view.SetViewModel(this);

			// todo: create when there a least one postprocessor exists. Postprocessors may come from plugins or it can be internal trace.
			mainFormPresenter.AddCustomTab(view.UIControl, TabCaption, this);
			mainFormPresenter.TabChanging += (sender, e) => this.changeNotification.Active = e.CustomTabTag == this;
		}

		public static string TabCaption => "Postprocessing";

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		IImmutableDictionary<ViewControlId, ControlData> IViewModel.ControlsState => getControlsData();

		void IViewModel.OnActionClick(string actionId, ViewControlId viewId, ClickFlags flags)
		{
			viewControlHandlers[viewId].ExecuteAction(actionId, flags);
		}

		private void InitAndAddProstprocessorHandler(
			ViewControlId postprocessorViewId,
			PostprocessorKind postprocessorKind
		)
		{
			IViewControlHandler handler;
			if (postprocessorViewId == ViewControlId.Correlate)
				handler = new CorrelatorPostprocessorControlHandler(
					correlationManager,
					tempFiles,
					shellOpen
				);
			else
				handler = new LogSourcePostprocessorControlHandler(
					postprocessorsManager,
					postprocessorKind,
					() =>
						postprocessorKind == PostprocessorKind.StateInspector ? presentersFactory.GetStateInspectorVisualizer(true) :
						postprocessorKind == PostprocessorKind.Timeline ? presentersFactory.GetTimelineVisualizer(true) :
						postprocessorKind == PostprocessorKind.SequenceDiagram ? presentersFactory.GetSequenceDiagramVisualizer(true) :
						postprocessorKind == PostprocessorKind.TimeSeries ? presentersFactory.GetTimeSeriesVisualizer(true) :
						(IPostprocessorVisualizerPresenter)null,
					shellOpen,
					tempFiles
				);
			viewControlHandlers.Add(postprocessorViewId, handler);
		}
	}
}
