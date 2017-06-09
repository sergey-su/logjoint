using System;
using Foundation;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class Node: NSObject
	{
		public string text;
		public object tag;
		public NodeColoring coloring;
		public Node parent;
		public readonly List<Node> children = new List<Node>();

		public NodesCollectionInfo ToNodesCollection()
		{
			return new NodesCollectionInfo () { Data = this };
		}

		public static Node FromNodesCollectionInfo(NodesCollectionInfo info)
		{
			return (Node)info.Data;
		}

		public NodeInfo ToNodeInfo()
		{
			return new NodeInfo ()
			{
				Data = this,
				Tag = this.tag,
				ChildrenNodesCollection = this.ToNodesCollection(),
				Text = this.text,
				Coloring = this.coloring
			};
		}

		public static Node FromNodeInfo(NodeInfo node)
		{
			return (Node)node.Data;
		}
	};
}

