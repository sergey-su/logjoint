using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing
{
    public class FailedRunSummary : IPostprocessorRunSummary, IStructuredPostprocessorRunSummary
    {
        readonly Exception exception;
        string report;

        public FailedRunSummary(Exception exception)
        {
            this.exception = exception;
        }

        bool IPostprocessorRunSummary.HasErrors
        {
            get { return true; }
        }

        bool IPostprocessorRunSummary.HasWarnings
        {
            get { return false; }
        }

        IPostprocessorRunSummary IPostprocessorRunSummary.GetLogSpecificSummary(ILogSource ls)
        {
            return null;
        }

        string IPostprocessorRunSummary.Report
        {
            get
            {
                if (report != null)
                    return report;
                var reportBuilder = new StringBuilder();
                LogJoint.LJTraceSource.WriteException(exception, reportBuilder);
                report = reportBuilder.ToString();
                return report;
            }
        }

        IEnumerable<(string text, bool isError, IBookmark bookmark)> IStructuredPostprocessorRunSummary.Entries
        {
            get
            {
                yield return (((IPostprocessorRunSummary)this).Report, true, null);
            }
        }

    };
}
