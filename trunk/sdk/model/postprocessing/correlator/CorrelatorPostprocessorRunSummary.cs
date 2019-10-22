namespace LogJoint.Postprocessing.Correlation
{
	public class CorrelatorPostprocessorRunSummary : IPostprocessorRunSummary // todo: move to model
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
