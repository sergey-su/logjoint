namespace LogJoint.Postprocessing.Correlator
{
	public class CorrelatorPostprocessorRunSummary : IPostprocessorRunSummary
	{
		bool correlationSucceeded;
		string details;

		public CorrelatorPostprocessorRunSummary(bool success, string details)
		{
			this.correlationSucceeded = success;
			this.details = details;
		}

		bool IPostprocessorRunSummary.HasErrors { get { return !correlationSucceeded; } }
		bool IPostprocessorRunSummary.HasWarnings { get { return false; } }
		string IPostprocessorRunSummary.Report { get { return details; } }
		IPostprocessorRunSummary IPostprocessorRunSummary.GetLogSpecificSummary(ILogSource ls) { return null; }
	};
}
