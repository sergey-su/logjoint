using System;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Postprocessing;

namespace LogJoint.Postprocessing.TimeSeries
{
    public class TimelineVisualizerModel : ITimeSeriesVisualizerModel
    {
        public TimelineVisualizerModel(
            IManagerInternal postprocessorsManager,
            ILogSourcesManager logSourcesManager,
            IUserNamesProvider shortNames,
            ILogSourceNamesProvider logSourceNamesProvider)
        {
            this.postprocessorsManager = postprocessorsManager;
            this.shortNames = shortNames;
            this.logSourceNamesProvider = logSourceNamesProvider;

            postprocessorsManager.Changed += (sender, args) => UpdateOutputs();
            logSourcesManager.OnLogSourceTimeOffsetChanged += (logSource, args) => UpdateAll();
            logSourcesManager.OnLogSourceVisiblityChanged += (logSource, args) => UpdateOutputs();

            UpdateOutputs();
        }

        public event EventHandler Changed;

        ICollection<ITimeSeriesPostprocessorOutput> ITimeSeriesVisualizerModel.Outputs
        {
            get { return outputs; }
        }

        void UpdateOutputs()
        {
            var newOutputs = new HashSet<ITimeSeriesPostprocessorOutput>(
                postprocessorsManager.LogSourcePostprocessors
                    .Where(output => output.OutputStatus == LogSourcePostprocessorState.Status.Finished || output.OutputStatus == LogSourcePostprocessorState.Status.Outdated)
                    .Select(output => output.OutputData)
                    .OfType<ITimeSeriesPostprocessorOutput>()
                    .Where(output => !output.LogSource.IsDisposed)
                    .Where(output => output.LogSource.Visible)
                );
            if (!newOutputs.SetEquals(outputs))
            {
                outputs = newOutputs;
                UpdateAll();
            }
        }

        private void UpdateAll()
        {
            FireChanged();
        }

        private void FireChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        readonly IManagerInternal postprocessorsManager;
        readonly IUserNamesProvider shortNames;
        readonly ILogSourceNamesProvider logSourceNamesProvider;
        HashSet<ITimeSeriesPostprocessorOutput> outputs = new HashSet<ITimeSeriesPostprocessorOutput>();
    };
}
