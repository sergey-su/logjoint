using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.UI.Presenters.ToolsContainer
{
    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        bool IsVisible { get; }
        IReadOnlyList<ToolInfo> AvailableTools { get; }
        int SelectedToolIndex { get; }
        double? Size { get; }

        void OnSelectTool(int index);
        void OnResize(double size);
        void OnHideButtonClicked();
        void OnShowButtonClicked();
    }

    public enum ToolKind
    {
        None,
        StateInspector,
        MessageProperties,
        SequenceDiagram
    }

    public struct ToolInfo
    {
        public ToolKind Kind;
        public string Name;
        public string Tooltip;
    };
}
