using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using LogJoint.Postprocessing.Correlation;
using LogJoint.UI.Presenters.Postprocessing.SummaryView;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public class Presenter: IPresenter, IViewModel
	{
		readonly IManagerInternal postprocessorsManager;
		readonly ICorrelationManager correlationManager;
		readonly IFactory presentersFactory;
		readonly Dictionary<ViewControlId, IViewControlHandler> viewControlHandlers = new Dictionary<ViewControlId, IViewControlHandler>();
		readonly ITempFilesManager tempFiles;
		readonly IShellOpen shellOpen;
		readonly IChainedChangeNotification changeNotification;
		readonly Func<IImmutableDictionary<ViewControlId, ControlData>> getControlsData;
		readonly Func<IImmutableDictionary<ViewControlId, ActionState>> getActionStates;

		public Presenter(
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
			viewControlHandlers.Add(ViewControlId.AllPostprocessors, new AllPostprocessorsControlHandler(postprocessorsManager, correlationManager));

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

			this.getActionStates = Selectors.Create(
				getControlsData,
				ctrlData => ImmutableDictionary.CreateRange(
					ctrlData.Select(h => {
						Action makeAction(string id) =>
							h.Value.Content.Contains($"*{id}")
							? () => viewControlHandlers[h.Key].ExecuteAction(id, ClickFlags.None)
							: (Action)null;
						return new KeyValuePair<ViewControlId, ActionState>(
							h.Key,
							new ActionState
							{
								Enabled = !h.Value.Disabled,
								Run = makeAction(Constants.RunActionId),
								Show = makeAction(Constants.ShowVisualizerActionId)
							}
						);
					})
				)
			);

			if (IsBrowser.Value)
			{
				this.changeNotification.Active = true;
			}
			else
			{
				mainFormPresenter.TabChanging += (sender, e) =>
				{
					this.changeNotification.Active = e.TabID == MainForm.TabIDs.Postprocessing;
				};
			}
		}

		public static string TabCaption => "Postprocessing";

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		IImmutableDictionary<ViewControlId, ControlData> IViewModel.ControlsState => getControlsData();

		ActionState IPresenter.StateInspector => getActionStates()[ViewControlId.StateInspector];
		ActionState IPresenter.Timeline => getActionStates()[ViewControlId.Timeline];
		ActionState IPresenter.SequenceDiagram => getActionStates()[ViewControlId.Sequence];
		ActionState IPresenter.TimeSeries => getActionStates()[ViewControlId.TimeSeries];
		ActionState IPresenter.Correlation => getActionStates()[ViewControlId.Correlate];

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
