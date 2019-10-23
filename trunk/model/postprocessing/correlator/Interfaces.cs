using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
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
			List<NodesConstraint> fixedConstraints,
			HashSet<string> allowInstacesMergingForRoles
		);
	};

	public class NodesConstraint // todo: hide from intfs? at leaast hist serialization.
	{
		public NodeId Node1, Node2;
		public TimeSpan Value;

		public NodesConstraint()
		{
		}

		internal static string xmlName = "nodesConstraint";

		internal XElement Serialize()
		{
			return new XElement(xmlName,
				new XElement("node1", Node1.Serialize()),
				new XElement("node2", Node2.Serialize()),
				new XAttribute("value", Value.Ticks)
			);
		}

		internal NodesConstraint(XElement node)
		{
			Node1 = new NodeId(node.Element("node1").Element(NodeId.xmlName));
			Node2 = new NodeId(node.Element("node2").Element(NodeId.xmlName));
			Value = TimeSpan.FromTicks(long.Parse(node.Attribute("value").Value));
		}
	};

	public interface ISolutionResult
	{
		SolutionStatus Status { get; }
		bool Success { get; }
		IReadOnlyDictionary<NodeId, NodeSolution> NodeSolutions { get; }
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
		public Messaging.Event RelatedMessagingEvent { get; private set; }
		internal MessageKey RelatedMessageKey { get; private set; }

		internal TimeDeltaEntry(DateTime at, TimeSpan delta, MessageKey messageKey, Messaging.Event evt)
		{
			At = at;
			Delta = delta;
			RelatedMessageKey = messageKey;
			RelatedMessagingEvent = evt;
		}
	};

	public class NodeSolution // todo: extract intf
	{
		public TimeSpan BaseDelta { get; private set; }
		public IReadOnlyList<TimeDeltaEntry> TimeDeltas { get; private set; }
		public int NrOnConstraints { get; private set; }
		public static string XmlName { get { return xmlName; } }

		internal static string xmlName = "solution";

		internal NodeSolution(TimeSpan baseDelta, IReadOnlyList<TimeDeltaEntry> timeDeltas, int nrOnConstraints)
		{
			BaseDelta = baseDelta;
			TimeDeltas = timeDeltas;
			NrOnConstraints = nrOnConstraints;
		}

		public XElement Serialize()
		{
			return new XElement(
				xmlName,
				new XAttribute("base-delta", BaseDelta.Ticks),
				new XAttribute("nr-of-constraints", NrOnConstraints),
				(TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>()).Select(d =>
					new XElement("delta",
						new XAttribute("at", d.At.Ticks),
						new XAttribute("value", d.Delta.Ticks)
					)
				)
			);
		}

		public NodeSolution(XElement node)
		{
			BaseDelta = TimeSpan.FromTicks(long.Parse(node.Attribute("base-delta").Value));
			NrOnConstraints = int.Parse(node.Attribute("nr-of-constraints").Value);
			TimeDeltas = node.Elements("delta").Select(de => new TimeDeltaEntry(
			   new DateTime(long.Parse(de.Attribute("at").Value), DateTimeKind.Unspecified),
			   TimeSpan.FromTicks(long.Parse(de.Attribute("value").Value)),
			   null,
			   null
		   )).ToList();
		}

		public bool Equals(NodeSolution other)
		{
			return
				BaseDelta == other.BaseDelta
				&& Enumerable.SequenceEqual(
					TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>(),
					other.TimeDeltas ?? Enumerable.Empty<TimeDeltaEntry>(),
					TimeDeltaEntryComparer.Instance
				);
		}

		class TimeDeltaEntryComparer : IEqualityComparer<TimeDeltaEntry>
		{
			public static TimeDeltaEntryComparer Instance = new TimeDeltaEntryComparer();

			bool IEqualityComparer<TimeDeltaEntry>.Equals(TimeDeltaEntry x, TimeDeltaEntry y)
			{
				return x.At == y.At && x.Delta == y.Delta;
			}

			int IEqualityComparer<TimeDeltaEntry>.GetHashCode(TimeDeltaEntry obj)
			{
				return obj.At.GetHashCode() ^ obj.Delta.GetHashCode();
			}
		};

	};
}
