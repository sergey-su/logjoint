using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
    class AllPostprocessorsControlHandler : IViewControlHandler
    {
        readonly IManagerInternal postprocessorsManager;
        readonly Func<LogSourcePostprocessorState[]> getOutputs;
        readonly Func<ControlData> getControlData;

        public AllPostprocessorsControlHandler(
            IManagerInternal postprocessorsManager,
            ICorrelationManager correlationManager
        )
        {
            this.postprocessorsManager = postprocessorsManager;
            this.getOutputs = Selectors.Create(
                () => postprocessorsManager.LogSourcePostprocessors,
                () => correlationManager.StateSummary,
                (outputs, correlatorSummary) =>
                    outputs.GetAutoPostprocessingCapableOutputs()
                    .Union(GetCorrelationOutputs(outputs, correlatorSummary))
                    .ToArray()
            );
            this.getControlData = Selectors.Create(
                getOutputs,
                outputs =>
                {
                    var relevantPostprocessorExists = outputs.Any();

                    return new ControlData(
                        !relevantPostprocessorExists,
                        "*action Run all postprocessors*",
                        ControlData.StatusColor.Neutral
                    );
                }
            );
        }

        ControlData IViewControlHandler.GetCurrentData() => getControlData();

        async void IViewControlHandler.ExecuteAction(string actionId, ClickFlags flags)
        {
            await postprocessorsManager.RunPostprocessors(getOutputs());
        }

        static IEnumerable<LogSourcePostprocessorState> GetCorrelationOutputs(
            IReadOnlyList<LogSourcePostprocessorState> outputs, CorrelationStateSummary status)
        {
            if (status.Status == CorrelationStateSummary.StatusCode.NeedsProcessing
             || status.Status == CorrelationStateSummary.StatusCode.Processed
             || status.Status == CorrelationStateSummary.StatusCode.ProcessingFailed)
            {
                return outputs.GetPostprocessorOutputsByPostprocessorId(PostprocessorKind.Correlator);
            }
            return Enumerable.Empty<LogSourcePostprocessorState>();
        }
    };
}
