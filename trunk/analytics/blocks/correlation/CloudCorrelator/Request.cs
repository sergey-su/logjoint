using LogJoint.Analytics.Messaging;
using LogJoint.Analytics.Messaging.Analisys;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Analytics.Correlation
{
	public class CloudCorrelationRequest
	{
		readonly Dictionary<NodeId, IEnumerable<Event>> nodes;
		readonly List<NodesConstraint> fixedConstraints;
		readonly HashSet<string> allowInstacesMergingForRoles;
		readonly internal static string xmlName = "request";

		public CloudCorrelationRequest(Dictionary<NodeId, IEnumerable<Event>> nodes, List<NodesConstraint> fixedConstraints, HashSet<string> allowInstacesMergingForRoles)
		{
			this.nodes = nodes;
			this.fixedConstraints = fixedConstraints;
			this.allowInstacesMergingForRoles = allowInstacesMergingForRoles;
		}

		public CloudCorrelationRequest(XDocument requestNode, Func<XElement, object> triggerDeserializer)
		{
			nodes = new Dictionary<NodeId, IEnumerable<Event>>();
			foreach (var node in requestNode.Root.Elements("node"))
			{
				nodes.Add(
					new NodeId(node.Element(NodeId.xmlName)),
					new EventsDeserializer(triggerDeserializer).Deserialize(node.Element("events")).ToArray()
				);
			}
			fixedConstraints = new List<NodesConstraint>();
			foreach (var node in requestNode.Root.Elements("fixedConstraints").Elements(NodesConstraint.xmlName))
			{
				fixedConstraints.Add(new NodesConstraint(node));
			}
			allowInstacesMergingForRoles = new HashSet<string>();
			foreach (var node in requestNode.Root.Elements("allowInstacesMergingForRoles").Elements("role"))
			{
				allowInstacesMergingForRoles.Add(node.Value);
			}
		}

		public Dictionary<NodeId, IEnumerable<Event>> Nodes { get { return nodes; } }

		public XDocument Serialize(Action<object, XElement> triggerSerializer)
		{
			var root = new XElement(xmlName);
			foreach (var node in nodes)
			{
				var serializer = new EventsSerializer(triggerSerializer, EventsSerializer.Flags.CoreMessageAttrsOnly);
				foreach (var e in node.Value)
					e.Visit(serializer);
				var eventsNode = new XElement("events");
				var nodeElement = new XElement(
					"node",
					node.Key.Serialize(),
					eventsNode
				);
				foreach (var e in serializer.Output)
					eventsNode.Add(e);
				root.Add(nodeElement);
			}
			root.Add(new XElement("fixedConstraints", 
				fixedConstraints.Select(fc => fc.Serialize())));
			root.Add(new XElement("allowInstacesMergingForRoles", 
				allowInstacesMergingForRoles.Select(role => new XElement("role", role))));
			return new XDocument(root);
		}
	};
}
