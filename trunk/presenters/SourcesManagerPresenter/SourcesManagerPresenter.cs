using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint;
using LogJoint.MRU;
using LogJoint.Preprocessing;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SourcesManager
{
    public class Presenter : IPresenter, IViewModel
    {
        public Presenter(
            ILogSourcesManager logSources,
            IUserDefinedFormatsManager udfManager,
            Preprocessing.IManager logSourcesPreprocessings,
            Workspaces.IWorkspacesManager workspacesManager,
            SourcesList.IPresenter sourcesListPresenter,
            NewLogSourceDialog.IPresenter newLogSourceDialogPresenter,
            IHeartBeatTimer heartbeat,
            SharingDialog.IPresenter sharingDialogPresenter,
            HistoryDialog.IPresenter historyDialogPresenter,
            IPresentersFacade facade,
            SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter,
            IAlertPopup alerts,
            ITraceSourceFactory traceSourceFactory,
            IChangeNotification changeNotification
        )
        {
            this.logSources = logSources;
            this.udfManager = udfManager;
            this.logSourcesPreprocessings = logSourcesPreprocessings;
            this.workspacesManager = workspacesManager;
            this.newLogSourceDialogPresenter = newLogSourceDialogPresenter;
            this.sourcesListPresenter = sourcesListPresenter;
            this.tracer = traceSourceFactory.CreateTraceSource("UI", "smgr-ui");
            this.sharingDialogPresenter = sharingDialogPresenter;
            this.historyDialogPresenter = historyDialogPresenter;
            this.sourcePropertiesWindowPresenter = sourcePropertiesWindowPresenter;
            this.alerts = alerts;
            this.presentersFacade = facade;
            this.changeNotification = changeNotification;

            sourcesListPresenter.DeleteRequested += (sender, args) =>
            {
                DeleteSelectedSources();
            };

            logSourcesPreprocessings.PreprocessingAdded += (sender, args) =>
            {
                if ((args.LogSourcePreprocessing.Flags & PreprocessingOptions.HighlightNewPreprocessing) != 0)
                {
                    preprocessingAwaitingHighlighting = args.LogSourcePreprocessing;
                    pendingUpdateFlag.Invalidate();
                }
            };

            heartbeat.OnTimer += (sender, args) =>
            {
                if (pendingUpdateFlag.Validate())
                    UpdateView();
            };

        }

        public event EventHandler<BusyStateEventArgs> OnBusyState;

        async void IPresenter.StartDeletionInteraction(ILogSource[] forSources)
        {
            await DeleteSources(forSources, Enumerable.Empty<ILogSourcePreprocessing>());
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.DeleteSelectedSourcesButtonEnabled =>
            (sourcesListPresenter.SelectedSources.Count + sourcesListPresenter.SelectedPreprocessings.Count) > 0;

        bool IViewModel.PropertiesButtonEnabled =>
            sourcePropertiesWindowPresenter != null && sourcesListPresenter.SelectedSources.Count == 1;

        bool IViewModel.DeleteAllSourcesButtonEnabled =>
            (logSources.Items.Count + logSourcesPreprocessings.Items.Count) > 0;

        (bool visible, bool enabled, bool progress) IViewModel.ShareButtonState =>
            (
                visible: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.PermanentlyUnavaliable,
                enabled: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.TemporarilyUnavailable,
                progress: sharingDialogPresenter.IsBusy
            );

        void IViewModel.OnAddNewLogButtonClicked()
        {
            udfManager.ReloadFactories(); // todo: move it away from this presenter

            newLogSourceDialogPresenter.ShowTheDialog();
        }

        void IViewModel.OnPropertiesButtonClicked()
        {
            var sel = sourcesListPresenter.SelectedSources.FirstOrDefault();
            if (sel != null && sourcePropertiesWindowPresenter != null)
                sourcePropertiesWindowPresenter.ShowWindow(sel);
        }

        void IViewModel.OnDeleteSelectedLogSourcesButtonClicked()
        {
            DeleteSelectedSources();
        }

        void IViewModel.OnDeleteAllLogSourcesButtonClicked()
        {
            DeleteAllSources();
            workspacesManager.DetachFromWorkspace();
        }

        void IViewModel.OnShowHistoryDialogButtonClicked()
        {
            historyDialogPresenter.ShowDialog();
        }

        void IViewModel.OnShareButtonClicked()
        {
            sharingDialogPresenter.ShowDialog();
        }

        #region Implementation

        void UpdateView()
        {
            HandlePendingHighlightings();
        }

        private void HandlePendingHighlightings()
        {
            if (preprocessingAwaitingHighlighting != null)
            {
                if (!preprocessingAwaitingHighlighting.IsDisposed)
                    presentersFacade.ShowPreprocessing(preprocessingAwaitingHighlighting);
                preprocessingAwaitingHighlighting = null;
            }
        }

        private async void DeleteSelectedSources()
        {
            await DeleteSources(sourcesListPresenter.SelectedSources, sourcesListPresenter.SelectedPreprocessings);
        }

        private async void DeleteAllSources()
        {
            await DeleteSources(logSources.Items, logSourcesPreprocessings.Items);
        }

        private async Task DeleteSources(IEnumerable<ILogSource> sourcesToDelete, IEnumerable<Preprocessing.ILogSourcePreprocessing> preprocessingToDelete)
        {
            using (tracer.NewFrame)
            {
                tracer.Info("----> User Command: Delete sources");

                var toDelete = new List<ILogSource>();
                var toDelete2 = new List<Preprocessing.ILogSourcePreprocessing>();
                foreach (ILogSource s in sourcesToDelete)
                {
                    tracer.Info("-- source to delete: {0}", s.ToString());
                    toDelete.Add(s);
                }
                foreach (Preprocessing.ILogSourcePreprocessing p in preprocessingToDelete)
                {
                    if (p.IsDisposed)
                        continue;
                    tracer.Info("-- preprocessing to delete: {0}", p.ToString());
                    toDelete2.Add(p);
                }

                if (toDelete.Count == 0 && toDelete2.Count == 0)
                {
                    tracer.Info("Nothing to delete");
                    return;
                }

                int goodItemsCount =
                    toDelete.Count(s => s.Provider.Stats.State != LogProviderState.LoadError) +
                    toDelete2.Count(p => p.Failure == null);
                if (goodItemsCount > 0) // do not ask about failed preprocessors or sources
                {
                    if (await alerts.ShowPopupAsync(
                        "Delete",
                        string.Format("Are you sure you want to close {0} log (s)", toDelete.Count + toDelete2.Count),
                        AlertFlags.YesNoCancel
                    ) != AlertFlags.Yes)
                    {
                        tracer.Info("User didn't confirm the deletion");
                        return;
                    }
                }

                SetWaitState(true);
                try
                {
                    await logSources.DeleteLogs(toDelete.ToArray());
                    await logSourcesPreprocessings.DeletePreprocessings(toDelete2.ToArray());
                }
                finally
                {
                    SetWaitState(false);
                }
            }
        }

        void SetWaitState(bool value)
        {
            OnBusyState?.Invoke(this, new BusyStateEventArgs(value));
        }

        readonly ILogSourcesManager logSources;
        readonly IUserDefinedFormatsManager udfManager;
        readonly Preprocessing.IManager logSourcesPreprocessings;
        readonly Workspaces.IWorkspacesManager workspacesManager;
        readonly SourcesList.IPresenter sourcesListPresenter;
        readonly NewLogSourceDialog.IPresenter newLogSourceDialogPresenter;
        readonly SharingDialog.IPresenter sharingDialogPresenter;
        readonly HistoryDialog.IPresenter historyDialogPresenter;
        readonly SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter;
        readonly LJTraceSource tracer;
        readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
        readonly IAlertPopup alerts;
        readonly IPresentersFacade presentersFacade;
        readonly IChangeNotification changeNotification;
        ILogSourcePreprocessing preprocessingAwaitingHighlighting;

        #endregion
    };
};