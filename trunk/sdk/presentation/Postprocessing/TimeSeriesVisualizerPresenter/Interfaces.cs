using LogJoint.Postprocessing.TimeSeries;
using System;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
    public interface IPresenter : IPostprocessorVisualizerPresenter
    {
        /// <summary>
        /// Opens the view that contains time series config. 
        /// </summary>
        void OpenConfigDialog();
        bool SelectConfigNode(Predicate<ITreeNodeData> predicate);
        bool ConfigNodeExists(Predicate<ITreeNodeData> predicate);
    };

    public enum ConfigDialogNodeType
    {
        Log,
        ObjectTypeGroup,
        ObjectIdGroup,
        TimeSeries,
        Events
    };

    public interface ITreeNodeData
    {
        ConfigDialogNodeType Type { get; }
        ILogSource LogSource { get; }
        string Caption { get; }
    };
}