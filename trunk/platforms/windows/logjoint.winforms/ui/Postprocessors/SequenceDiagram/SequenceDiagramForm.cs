using LogJoint.UI;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
    public partial class SequenceDiagramForm : ToolForm
    {
        public SequenceDiagramForm()
        {
            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(UIUtils.Dpi.Scale(950, 120), UIUtils.Dpi.Scale(450, 120));
        }

        public LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer.IView SequenceDiagramVisualizerView { get { return sequenceDiagramVisualizerControl1; } }
    }
}
