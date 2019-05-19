using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public class PluginTabPagePresenter: IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IPostprocessorsManager postprocessorsManager;
		readonly IPostprocessorOutputFormFactory outputFormsFactory;
		readonly Dictionary<ViewControlId, IViewControlHandler> viewControlHandlers = new Dictionary<ViewControlId, IViewControlHandler>();
		readonly ITempFilesManager tempFiles;
		readonly IShellOpen shellOpen;
		readonly NewLogSourceDialog.IPresenter newLogSourceDialog;
		readonly List<IViewControlHandler> logsCollectionControlHandlers = new List<IViewControlHandler>();
		readonly Telemetry.ITelemetryCollector telemetry;
		bool initialized;

		public PluginTabPagePresenter(
			IView view,
			IPostprocessorsManager postprocessorsManager,
			IPostprocessorOutputFormFactory outputFormsFactory,
			ILogSourcesManager logSourcesManager,
			ITempFilesManager tempFiles,
			IShellOpen shellOpen,
			NewLogSourceDialog.IPresenter newLogSourceDialog,
			Telemetry.ITelemetryCollector telemetry
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.postprocessorsManager = postprocessorsManager;
			this.outputFormsFactory = outputFormsFactory;
			this.tempFiles = tempFiles;
			this.shellOpen = shellOpen;
			this.newLogSourceDialog = newLogSourceDialog;
			this.telemetry = telemetry;

			logSourcesManager.OnLogSourceAnnotationChanged += (sender, e) =>
			{
				RefreshView();
			};
		}


		void IPresenter.AddLogsCollectionControlHandler(IViewControlHandler value)
		{
			logsCollectionControlHandlers.Add(value);
		}

		void IViewEvents.OnTabPageSelected()
		{
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

		void IViewEvents.OnActionClick(string actionId, ViewControlId viewId, ClickFlags flags)
		{
			viewControlHandlers[viewId].ExecuteAction(actionId, flags);
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

			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.StateInspector, pm, outputFormsFactory, PostprocessorKind.StateInspector);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Timeline, pm, outputFormsFactory, PostprocessorKind.Timeline);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Sequence, pm, outputFormsFactory, PostprocessorKind.SequenceDiagram);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.Correlate, pm, outputFormsFactory, PostprocessorKind.Correlator);
			InitAndAddProstprocessorHandler(viewControlHandlers, ViewControlId.TimeSeries, pm, outputFormsFactory, PostprocessorKind.TimeSeries);

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
			IPostprocessorsManager postprocessorsManager,
			IPostprocessorOutputFormFactory outputFormsFactory,
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
					() => outputFormsFactory.GetPostprocessorOutputForm(postprocessorViewId),
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

			view.BeginBatchUpdate();
			foreach (var h in viewControlHandlers)
			{
				view.UpdateControl(h.Key, h.Value.GetCurrentData());
			}
			view.EndBatchUpdate();
		}
	}
}
