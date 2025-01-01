using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer.MenuData;

namespace LogJoint.UI.Presenters.ToolsContainer
{
    class Presenter : IViewModel, IPresenter
    {
        readonly IChangeNotification changeNotification;
        bool isVisible = false;
        double? size = null;
        int selectedToolIndex = 0;
        readonly IReadOnlyList<ToolKind> availableTools = new[] { ToolKind.StateInspector, ToolKind.Timeline, ToolKind.MessageProperties };
        readonly Func<IReadOnlyList<ToolInfo>> availableToolsInfo;
        private readonly Task<Persistence.IStorageEntry> globalSettings;
        private readonly TaskChain tasks = new TaskChain();
        private readonly static string SettingsKey = "main-window-tools-container";
        private readonly static string rootNodeName = "tools-container";
        private readonly static string visibleAttrName = "visible";
        private readonly static string sizeAttrName = "size";

        public Presenter(
            IChangeNotification changeNotification,
            Task<Persistence.IStorageEntry> globalSettings,
            IShutdown shutdown
        )
        {
            this.changeNotification = changeNotification;
            this.globalSettings = globalSettings;
            shutdown.Cleanup += (s, e) => shutdown.AddCleanupTask(tasks.Dispose());
            this.availableToolsInfo = Selectors.Create(
                () => availableTools,
                kinds => ImmutableList.CreateRange(kinds.Select(ToToolInfo))
            );
            tasks.AddTask(LoadState);
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.IsVisible => isVisible;

        IReadOnlyList<ToolInfo> IViewModel.AvailableTools => availableToolsInfo();

        int IViewModel.SelectedToolIndex => selectedToolIndex;

        double? IViewModel.Size => size;

        void IViewModel.OnHideButtonClicked()
        {
            if (isVisible)
            {
                isVisible = false;
                SaveState();
                changeNotification.Post();
            }
        }

        void IViewModel.OnResize(double size)
        {
            if (isVisible)
            {
                this.size = Math.Max(0, size);
                SaveState();
                changeNotification.Post();
            }
        }

        void IViewModel.OnSelectTool(int index)
        {
            selectedToolIndex = index;
            changeNotification.Post();
        }

        void IViewModel.OnShowButtonClicked()
        {
            if (!isVisible)
            {
                isVisible = true;
                SaveState();
                changeNotification.Post();
            }
        }

        string IViewModel.HideButtonTooltip => "Hide tools panel";

        string IViewModel.ShowButtonTooltip => "Show tools panel";

        string IViewModel.ResizerTooltip => "Resize tools panel";

        void IPresenter.ShowTool(ToolKind kind)
        {
            var i = availableTools.IndexOf(k => k == kind);
            if (i.HasValue)
            {
                isVisible = true;
                selectedToolIndex = i.Value;
                changeNotification.Post();
            }
        }

        static ToolInfo ToToolInfo(ToolKind kind)
        {
            return kind switch
            {
                ToolKind.StateInspector => new ToolInfo { Kind = kind, Name = "StateInspector", Tooltip = null },
                ToolKind.MessageProperties => new ToolInfo { Kind = kind, Name = "Log message", Tooltip = null },
                ToolKind.Timeline => new ToolInfo { Kind = kind, Name = "Timeline", Tooltip = null },
                _ => new ToolInfo { Kind = kind, Name = "?", Tooltip = "?" },
            };
        }

        private async Task LoadState()
        {
            await using var section = await (await globalSettings).OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadOnly);
            isVisible = section.Data.Element(rootNodeName).SafeIntValue(visibleAttrName, 0) == 1;
            size = section.Data.Element(rootNodeName)?.DoubleValue(sizeAttrName);
            changeNotification.Post();
        }

        private void SaveState()
        {
            tasks.AddTask(async () =>
            {
                var state = new XElement(rootNodeName);
                state.SetAttributeValue(visibleAttrName, isVisible ? 1 : 0);
                if (size.HasValue)
                    state.SetAttributeValue(sizeAttrName, size.Value);
                await using var section = await (await globalSettings).OpenXMLSection(SettingsKey, Persistence.StorageSectionOpenFlag.ReadWrite);
                section.Data.RemoveNodes();
                section.Data.Add(state);
            });
        }

    }
}
