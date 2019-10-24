using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
	public interface ICorrelationManager
	{
		CorrelationStateSummary StateSummary { get; }
		void Run();
	};

	public interface ICorrelatorOutput : IPostprocessorOutputETag
	{
		ILogSource LogSource { get; }
		NodeId NodeId { get; } // todo: good vocabulary?
		IEnumerable<M.Event> Events { get; }
		ILogPartToken RotatedLogPartToken { get; }
		ISameNodeDetectionToken SameNodeDetectionToken { get; }
	};

	public interface ICorrelator
	{
		Task<ISolutionResult> Correlate(
			Dictionary<NodeId,
			IEnumerable<M.Event>> input,
			List<FixedConstraint> fixedConstraints,
			HashSet<string> allowInstacesMergingForRoles
		);
	};

	public class FixedConstraint
	{
		public NodeId Node1, Node2;
		public TimeSpan Value;
	};

	public interface ISolutionResult
	{
		SolutionStatus Status { get; }
		bool Success { get; }
		IReadOnlyDictionary<NodeId, INodeSolution> NodeSolutions { get; }
		string ToString(string format);
		string CorrelationLog { get; }
	};

	public enum SolutionStatus
	{
		Solved,
		Infeasible,
		NoInternodeMessages,
		Timeout
	};

	public class TimeDeltaEntry
	{
		public DateTime At { get; private set; }
		public TimeSpan Delta { get; private set; }
		public M.Event RelatedMessagingEvent { get; private set; }
		internal MessageKey RelatedMessageKey { get; private set; }

		internal TimeDeltaEntry(DateTime at, TimeSpan delta, MessageKey messageKey, M.Event evt)
		{
			At = at;
			Delta = delta;
			RelatedMessageKey = messageKey;
			RelatedMessagingEvent = evt;
		}
	};

	public interface INodeSolution
	{
		TimeSpan BaseDelta { get; }
		IReadOnlyList<TimeDeltaEntry> TimeDeltas { get; }
		int NrOnConstraints { get; }
		XElement Serialize();
		bool Equals(INodeSolution other);
	};

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
