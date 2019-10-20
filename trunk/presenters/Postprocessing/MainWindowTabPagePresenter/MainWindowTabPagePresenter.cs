using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public class Presenter: IPresenter, IViewModel
	{
		readonly IView view;
		readonly IManager postprocessorsManager;
		readonly IFactory presentersFactory;
		readonly Dictionary<ViewControlId, IViewControlHandler> viewControlHandlers = new Dictionary<ViewControlId, IViewControlHandler>();
		readonly ITempFilesManager tempFiles;
		readonly IShellOpen shellOpen;
		readonly NewLogSourceDialog.IPresenter newLogSourceDialog;
		readonly List<IViewControlHandler> logsCollectionControlHandlers = new List<IViewControlHandler>();
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IChainedChangeNotification changeNotification;
		bool initialized;
		IImmutableDictionary<ViewControlId, ControlData> controlsData = ImmutableDictionary.Create<ViewControlId, ControlData>();

		public Presenter(
			IView view,
			IManager postprocessorsManager,
			IFactory presentersFactory,
			ILogSourcesManager logSourcesManager,
			ITempFilesManager tempFiles,
			IShellOpen shellOpen,
			NewLogSourceDialog.IPresenter newLogSourceDialog,
			Telemetry.ITelemetryCollector telemetry,
			IChangeNotification changeNotification,
			MainForm.IPresenter mainFormPresenter
		)
		{
			this.view = view;
			this.postprocessorsManager = postprocessorsManager;
			this.presentersFactory = presentersFactory;
			this.tempFiles = tempFiles;
			this.shellOpen = shellOpen;
			this.newLogSourceDialog = newLogSourceDialog;
			this.telemetry = telemetry;
			this.changeNotification = changeNotification.CreateChainedChangeNotification(false);

			this.view.SetViewModel(this);

			logSourcesManager.OnLogSourceAnnotationChanged += (sender, e) =>
			{
				RefreshView();
			};

			// todo: create when there a least one postprocessor exists. Postprocessors may come from plugins or it can be internal trace.

			mainFormPresenter.AddCustomTab(view.UIControl, TabCaption, this);
			mainFormPresenter.TabChanging += (sender, e) => OnTabPageSelected(e.CustomTabTag == this);
		}

		public static string TabCaption => "Postprocessing";

		void IPresenter.AddLogsCollectionControlHandler(IViewControlHandler value)
		{
			logsCollectionControlHandlers.Add(value);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;
		IImmutableDictionary<ViewControlId, ControlData> IViewModel.ControlsState => controlsData;

		void IViewModel.OnActionClick(string actionId, ViewControlId viewId, ClickFlags flags)
		{
			viewControlHandlers[viewId].ExecuteAction(actionId, flags);
		}

		void OnTabPageSelected(bool selected)
		{
			changeNotification.Active = selected;
			if (!selected)
				return;
			try
			{
				EnsureInitialized();
				RefreshView();
			}
			catch (Exception e)
			{
				telemetry.ReportException(e, "postprocessors tab page activation failed");
			}
		}

		void EnsureInitialized()
		{
			if (initialized)
				return;
			initialized = true;

			var pm = postprocessorsManager;

			pm.Changed += delegate(object sender, EventArgs e)
			{
				RefreshView();
			};

			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.StateInspector, presentersFactory, PostprocessorKind.StateInspector);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Timeline, presentersFactory, PostprocessorKind.Timeline);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Sequence, presentersFactory, PostprocessorKind.SequenceDiagram);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Correlate, presentersFactory, PostprocessorKind.Correlator);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.TimeSeries, presentersFactory, PostprocessorKind.TimeSeries);

			foreach (var h in 
				(logsCollectionControlHandlers.Count == 0 ? new IViewControlHandler[] { new GenericLogsOpenerControlHandler(newLogSourceDialog) } : logsCollectionControlHandlers.ToArray())
				.Take(ViewControlId.LogsCollectionControl3 - ViewControlId.LogsCollectionControl1 + 1).Select((h, i) => new { h, i }))
			{
				AddLogsCollectionHandler(ViewControlId.LogsCollectionControl1 + h.i, h.h);
			}
			
			viewControlHandlers.Add(ViewControlId.AllPostprocessors, new AllPostprocessorsControlHandler(pm));
		}

		private void InitAndAddProstprocessorHandler(
			Dictionary<ViewControlId, IViewControlHandler> handlers,
			ViewControlId postprocessorViewId,
			IFactory factory,
			PostprocessorKind postprocessorKind
		)
		{
			IViewControlHandler handler;
			if (postprocessorViewId == ViewControlId.Correlate)
				handler = new CorrelatorPostprocessorControlHandler(
					postprocessorsManager,
					tempFiles,
					shellOpen
				);
			else
				handler = new LogSourcePostprocessorControlHandler(
					postprocessorsManager,
					postprocessorKind,
					() =>
						postprocessorKind == PostprocessorKind.StateInspector ? factory.GetStateInspectorVisualizer(true) :
						postprocessorKind == PostprocessorKind.Timeline ? factory.GetTimelineVisualizer(true) :
						postprocessorKind == PostprocessorKind.SequenceDiagram ? factory.GetSequenceDiagramVisualizer(true) :
						postprocessorKind == PostprocessorKind.TimeSeries ? factory.GetTimeSeriesVisualizer(true) :
						(IPostprocessorVisualizerPresenter)null,
					shellOpen,
					tempFiles
				);
			handlers.Add(postprocessorViewId, handler);
		}

		void AddLogsCollectionHandler(
			ViewControlId controlId,
			IViewControlHandler handler
		)
		{
			viewControlHandlers.Add(controlId, handler);
		}

		private void RefreshView()
		{
			if (!initialized)
				return;
			controlsData = ImmutableDictionary.CreateRange(
				viewControlHandlers.Select(h => new KeyValuePair<ViewControlId, ControlData>(h.Key, h.Value.GetCurrentData()))
			);
			changeNotification.Post();
		}
	}
}
