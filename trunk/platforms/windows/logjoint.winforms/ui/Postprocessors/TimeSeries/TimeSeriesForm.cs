using LogJoint.UI;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
    public partial class TimeSeriesForm : ToolForm
    {
        public TimeSeriesForm()
        {
            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(UIUtils.Dpi.Scale(950, 120), UIUtils.Dpi.Scale(450, 120));
        }

        public LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer.IView TimeSeriesVisualizerView { get { return timeSeriesVisualizer; } }
    }
}
