using System;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	public interface IPostprocessorRunSummary
	{
		bool HasErrors { get; }
		bool HasWarnings { get; }
		string Report { get; }
		IPostprocessorRunSummary GetLogSpecificSummary(ILogSource ls);
	};
}
