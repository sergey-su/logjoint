using System;
using System.Collections.Generic;

// todo: merge with correlator

namespace LogJoint.Postprocessing
{

	public interface ICorrelationManager
	{
		Correlation.CorrelationStateSummary StateSummary { get; }
		void Run();
	};

	namespace Correlation
	{

		public struct CorrelationStateSummary // todo: make immutable class
		{
			public enum StatusCode
			{
				PostprocessingUnavailable,
				NeedsProcessing,
				ProcessingInProgress,
				Processed,
				ProcessingFailed
			};

			public StatusCode Status;
			public double? Progress;
			public string Report;
		};
	}
}
