using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing
{
	public class AggregatedRunSummary: IPostprocessorRunSummary
	{
		string report;
		bool hasErrors, hasWarnings;
		readonly Dictionary<ILogSource, IPostprocessorRunSummary> innerSummaries;
	
		public AggregatedRunSummary(
			Dictionary<ILogSource, IPostprocessorRunSummary> innerSummaries
		)
		{
			this.innerSummaries = innerSummaries;
			var stringBuilder = new StringBuilder();
			foreach (var x in innerSummaries)
			{
				stringBuilder.AppendLine(x.Key.GetShortDisplayNameWithAnnotation());
				stringBuilder.Append(x.Value.Report ?? "");
				stringBuilder.AppendLine();
				hasErrors = hasErrors || x.Value.HasErrors;
				hasWarnings = hasWarnings || x.Value.HasWarnings;
			}
			report = stringBuilder.ToString();
		}

		bool IPostprocessorRunSummary.HasErrors
		{
			get { return hasErrors; }
		}

		bool IPostprocessorRunSummary.HasWarnings
		{
			get { return hasWarnings; }
		}

		string IPostprocessorRunSummary.Report
		{
			get { return report; }
		}
		
		IPostprocessorRunSummary IPostprocessorRunSummary.GetLogSpecificSummary(ILogSource ls)
		{
			IPostprocessorRunSummary ret;
			innerSummaries.TryGetValue(ls, out ret);
			return ret;
		}
	};
}
