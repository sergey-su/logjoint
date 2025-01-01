
using LogJoint.UI;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
    public partial class TimelineForm : ToolForm
    {
        public TimelineForm()
        {
            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(UIUtils.Dpi.Scale(770, 120), UIUtils.Dpi.Scale(430, 120));
        }

        public LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.IView TimelineVisualizerView { get { return timelineVisualizerControl1; } }
    }
}
