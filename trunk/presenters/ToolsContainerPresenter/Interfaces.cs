﻿using System;
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
        string HideButtonTooltip { get; }
        string ShowButtonTooltip { get; }
        string ResizerTooltip { get; }

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
        SequenceDiagram,
        Timeline,
    }

    public struct ToolInfo
    {
        public ToolKind Kind;
        public string Name;
        public string Tooltip;
    }

    public interface IPresenter
    {
        void ShowTool(ToolKind kind);
    }
}
