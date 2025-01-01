﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.Persistence;
using LogJoint.MRU;
using ICSharpCode.SharpZipLib.Zip;

namespace LogJoint.Workspaces
{
    public class WorkspacesManager : IWorkspacesManager
    {
        readonly LJTraceSource tracer;
        readonly ILogSourcesManager logSources;
        readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
        readonly Backend.IBackendAccess backendAccess;
        readonly IStorageManager storageManager;
        readonly ITempFilesManager tempFilesManager;
        readonly MRU.IRecentlyUsedEntities recentlyUsedEntities;
        WorkspacesManagerStatus status;
        WorkspaceInfo currentWorkspace;
        string lastError;

        public WorkspacesManager(
            ILogSourcesManager logSources,
            ILogProviderFactoryRegistry logProviderFactoryRegistry,
            IStorageManager storageManager,
            Backend.IBackendAccess backend,
            ITempFilesManager tempFilesManager,
            MRU.IRecentlyUsedEntities recentlyUsedEntities,
            IShutdown shutdown,
            ITraceSourceFactory traceSourceFactory
        )
        {
            this.tracer = traceSourceFactory.CreateTraceSource("Workspaces", "ws");
            this.logSources = logSources;
            this.backendAccess = backend;
            this.tempFilesManager = tempFilesManager;
            this.logProviderFactoryRegistry = logProviderFactoryRegistry;
            this.storageManager = storageManager;
            this.recentlyUsedEntities = recentlyUsedEntities;
            if (backend.IsConfigured)
                this.status = WorkspacesManagerStatus.NoWorkspace;
            else
                this.status = WorkspacesManagerStatus.Unavailable;
            shutdown.Cleanup += (s, e) => shutdown.AddCleanupTask(
                WaitUploadCompletion().WithTimeout(TimeSpan.FromSeconds(10)));
        }

        bool IWorkspacesManager.IsWorkspaceUri(Uri uri)
        {
            return backendAccess.IsValidWorkspaceUri(uri);
        }

        WorkspacesManagerStatus IWorkspacesManager.Status
        {
            get { return status; }
        }

        WorkspaceInfo IWorkspacesManager.CurrentWorkspace
        {
            get { return currentWorkspace; }
        }

        string IWorkspacesManager.LastError
        {
            get { return lastError; }
        }

        void IWorkspacesManager.DetachFromWorkspace()
        {
            if (status != WorkspacesManagerStatus.Unavailable)
            {
                SetCurrentWorkspace(null);
                SetStatus(WorkspacesManagerStatus.NoWorkspace);
            }
        }

        public event EventHandler StatusChanged;
        public event EventHandler CurrentWorkspaceChanged;

        async Task IWorkspacesManager.SaveWorkspace(string name, string annotation)
        {
            WorkspaceInfo initialWorkspace = currentWorkspace;
            var entriesStreams = new List<KeyValuePair<string, Stream>>();
            try
            {
                SetLastError(null);
                SetStatus(WorkspacesManagerStatus.CreatingWorkspace);

                SetCurrentWorkspace(CreateWorkspaceInfo(name, annotation));

                var createdWs = await CreateWorkspace(
                    name, annotation, initialWorkspace != null && initialWorkspace.Name == name, entriesStreams);

                SetCurrentWorkspace(CreateWorkspaceInfoForJustCreatedWs(createdWs, annotation));

                SetStatus(WorkspacesManagerStatus.SavingWorkspaceData);

                await UploadEntriesArchive(createdWs.entriesArchiveUrl, await CreateEntriesArchive(entriesStreams));

                SetStatus(WorkspacesManagerStatus.AttachedToUploadedWorkspace);

                recentlyUsedEntities.RegisterRecentWorkspaceEntry(createdWs.selfUrl, createdWs.id, annotation);
            }
            catch (Exception e)
            {
                tracer.Error(e, "failed to save ws");
                SetLastError(e.Message);
                SetStatus(WorkspacesManagerStatus.FailedToUploadWorkspace);
            }
            finally
            {
                entriesStreams.ForEach(e => e.Value.Dispose());
            }
        }

        async Task<WorkspaceEntryInfo[]> IWorkspacesManager.LoadWorkspace(string workspaceUri, CancellationToken cancellation)
        {
            try
            {
                SetStatus(WorkspacesManagerStatus.LoadingWorkspace);
                SetLastError(null);

                WorkspaceDTO workspace = await backendAccess.GetWorkspace(workspaceUri, cancellation);

                await LoadEmbeddedStorageEntries(workspace, cancellation);

                SetCurrentWorkspace(new WorkspaceInfo()
                {
                    Name = workspace.id,
                    Annotation = workspace.annotation,
                    WebUrl = workspace.selfLaunchUrl
                });
                SetStatus(WorkspacesManagerStatus.LoadingWorkspaceData);

                await LoadArchivedStorageEntries(workspace.entriesArchiveUrl, cancellation);

                if (backendAccess.IsConfigured)
                    SetStatus(WorkspacesManagerStatus.AttachedToDownloadedWorkspace);
                else
                    SetStatus(WorkspacesManagerStatus.Unavailable);

                recentlyUsedEntities.RegisterRecentWorkspaceEntry(workspace.selfUrl, workspace.id, workspace.annotation);

                return workspace
                    .sources
                    .Select(source => new WorkspaceEntryInfo()
                    {
                        Log = new RecentLogEntry(logProviderFactoryRegistry, source.connectionString, null, null),
                        IsHiddenLog = source.hidden
                    })
                    .ToArray();
            }
            catch (Exception e)
            {
                tracer.Error(e, "failed to load ws '{0}'", workspaceUri);
                SetLastError(e.Message);
                if (backendAccess.IsConfigured)
                    SetStatus(WorkspacesManagerStatus.FailedToDownloadWorkspace);
                else
                    SetStatus(WorkspacesManagerStatus.Unavailable);

                return new WorkspaceEntryInfo[] { };
            }
        }

        private async Task LoadArchivedStorageEntries(string entriesArchiveUrl, CancellationToken cancellation)
        {
            var entriesArchiveFileName = tempFilesManager.GenerateNewName();
            using (var entriesArchiveStream = new FileStream(entriesArchiveFileName, FileMode.CreateNew))
            {
                await backendAccess.GetEntriesArchive(entriesArchiveUrl, entriesArchiveStream, cancellation);
            }
            var sectionContentTempFileName = tempFilesManager.GenerateNewName();
            var entries = new Dictionary<string, IStorageEntry>();
            using (var zipFile = new ZipFile(entriesArchiveFileName))
            {
                foreach (var zipEntry in zipFile.OfType<ZipEntry>().Where(e => e != null))
                {
                    if (zipEntry.IsDirectory)
                        continue;
                    var storageEntryId = Path.GetDirectoryName(zipEntry.Name);
                    var sectionId = Path.GetFileName(zipEntry.Name);
                    using (var sectionContentStream = new FileStream(sectionContentTempFileName,
                        FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    {
                        using (var inputStream = zipFile.GetInputStream(zipEntry))
                            IOUtils.CopyStreamWithProgress(inputStream, sectionContentStream, _ => { }, CancellationToken.None);
                        sectionContentStream.Position = 0;
                        IStorageEntry storageEntry;
                        if (!entries.TryGetValue(storageEntryId, out storageEntry))
                            entries.Add(storageEntryId, storageEntry = await storageManager.GetEntryById(storageEntryId));
                        await storageEntry.LoadSectionFromSnapshot(sectionId, sectionContentStream, cancellation);
                    }
                }
            }

        }

        private async Task LoadEmbeddedStorageEntries(WorkspaceDTO workspace, CancellationToken cancellation)
        {
            foreach (var embeddedStorageEntry in workspace.embeddedStorageEntries)
            {
                var entry = await storageManager.GetEntryById(embeddedStorageEntry.id);
                foreach (var embeddedStorageSection in embeddedStorageEntry.sections)
                    using (var sectionStream = new MemoryStream(Convert.FromBase64String(embeddedStorageSection.value)))
                        await entry.LoadSectionFromSnapshot(embeddedStorageSection.id, sectionStream, cancellation);
            }

        }

        void SetStatus(WorkspacesManagerStatus status)
        {
            if (this.status == status)
                return;
            tracer.Info("status {0}->{1}", this.status, status);
            this.status = status;
            if (StatusChanged != null)
                StatusChanged(this, EventArgs.Empty);
        }

        void SetLastError(string value)
        {
            if (this.lastError == value)
                return;
            tracer.Info("last error -> {0}", value);
            lastError = value;
        }

        void SetCurrentWorkspace(WorkspaceInfo ws)
        {
            if (ws == null)
                tracer.Info("current workspace -> null");
            else
                tracer.Info("current workspace -> ({0} {1})", ws.Name, ws.Uri);
            this.currentWorkspace = ws;
            if (CurrentWorkspaceChanged != null)
                CurrentWorkspaceChanged(this, EventArgs.Empty);
        }

        private async Task UploadEntriesArchive(string entriesArchiveUrl, string entriesArchiveFileName)
        {
            using (var entriesArchiveStream = new FileStream(
                entriesArchiveFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                await backendAccess.UploadEntriesArchive(entriesArchiveUrl, entriesArchiveStream);
            }
        }

        private static WorkspaceInfo CreateWorkspaceInfo(string name, string annotation)
        {
            return new WorkspaceInfo()
            {
                Name = name,
                Uri = null,
                WebUrl = null,
                Annotation = annotation
            };
        }

        private static WorkspaceInfo CreateWorkspaceInfoForJustCreatedWs(CreatedWorkspaceDTO createdWs, string annotation)
        {
            var nameAlterationReason = WorkspaceNameAlterationReason.None;
            if (createdWs.idAlterationReason != null)
                if (createdWs.idAlterationReason.Value == IdAlterationReason.conflict)
                    nameAlterationReason = WorkspaceNameAlterationReason.Conflict;
                else if (createdWs.idAlterationReason.Value == IdAlterationReason.validation)
                    nameAlterationReason = WorkspaceNameAlterationReason.InvalidName;
            var newWs = new WorkspaceInfo()
            {
                Name = createdWs.id,
                Uri = createdWs.selfUrl,
                WebUrl = createdWs.selfWebUrl,
                Annotation = annotation,
                NameAlterationReason = nameAlterationReason
            };
            return newWs;
        }

        private async Task<string> CreateEntriesArchive(List<KeyValuePair<string, Stream>> entriesToArchive)
        {
            var entriesArchiveFileName = tempFilesManager.GenerateNewName();
            await Task.Run(() =>
            {
                using (var baseOutputStream = new FileStream(entriesArchiveFileName, FileMode.Create))
                using (var zip = new ZipOutputStream(baseOutputStream))
                {
                    foreach (var entry in entriesToArchive)
                    {
                        var newEntry = new ZipEntry(entry.Key);
                        zip.PutNextEntry(newEntry);
                        entry.Value.Position = 0;
                        IOUtils.CopyStreamWithProgress(entry.Value, zip, _ => { }, CancellationToken.None);
                        zip.CloseEntry();
                    }
                }
            });
            return entriesArchiveFileName;
        }

        private async Task<CreatedWorkspaceDTO> CreateWorkspace(
            string name, string annotation, bool allowOverwrite,
            List<KeyValuePair<string, Stream>> entriesToArchive)
        {
            var dto = new WorkspaceDTO()
            {
                id = name,
                annotation = annotation,
                allowOverwrite = allowOverwrite
            };

            var sources = logSources.Items.ToArray();

            dto.sources.AddRange(sources.Select(source => new WorkspaceDTO.Source()
            {
                connectionString = new RecentLogEntry(source.Provider.Factory, source.Provider.ConnectionParams, source.Annotation, null).ToString(),
                hidden = !source.Visible
            }));
            var entries = sources.Select(logSource => logSource.LogSourceSpecificStorageEntry).ToArray();
            var entriesArchiveFolderName = tempFilesManager.GenerateNewName();
            Directory.CreateDirectory(entriesArchiveFolderName);
            foreach (var entry in entries)
            {
                var dtoEntry = new WorkspaceDTO.EmbeddedStorageEntry()
                {
                    id = entry.Id
                };
                dto.embeddedStorageEntries.Add(dtoEntry);
                var sections = entry.EnumSections(CancellationToken.None);
                await foreach (var section in sections)
                {
                    if (section.Key == "bookmarks" || section.Key == "settings") // todo: hardcoded strings
                    {
                        using (var stream = new MemoryStream())
                        {
                            await entry.TakeSectionSnapshot(section.Id, stream);
                            dtoEntry.sections.Add(new WorkspaceDTO.EmbeddedStorageSection()
                            {
                                id = section.Id,
                                value = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length, Base64FormattingOptions.None)
                            });
                        }
                    }
                    else
                    {
                        var tempFileStream = new FileStream(
                            Path.Combine(entriesArchiveFolderName, entriesToArchive.Count.ToString()),
                            FileMode.CreateNew,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            4096,
                            FileOptions.DeleteOnClose);
                        await entry.TakeSectionSnapshot(section.Id, tempFileStream);
                        entriesToArchive.Add(new KeyValuePair<string, Stream>(
                            entry.Id + Path.DirectorySeparatorChar + section.Id, tempFileStream));
                    }
                }
            }

            var createdWs = await backendAccess.CreateWorkspace(dto);
            return createdWs;
        }

        async Task WaitUploadCompletion()
        {
            while (status == WorkspacesManagerStatus.CreatingWorkspace || status == WorkspacesManagerStatus.SavingWorkspaceData)
                await Task.Delay(100);
        }
    };
}