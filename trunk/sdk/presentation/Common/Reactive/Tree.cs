using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Reactive
{
	/// <summary>
	/// A tree that is built of these nodes can participate in reactive updates.
	/// If a presenter returns tree data as ITreeNode-rooted tree,
	/// it's possible for particular platform-specific UI to blindly
	/// follow the changes in the tree.
	/// The ITreeNode objects must be immutable.
	/// </summary>
	public interface ITreeNode
	{
		/// <summary>
		/// The key is used by UI update procedures to determines if
		/// a node from tree older version matches that in new version.
		/// </summary>
		string Key { get; }
		/// <summary>
		/// List of children node. Must not be null.
		/// </summary>
		IReadOnlyList<ITreeNode> Children { get; }
		/// <summary>
		/// Determines if tree node is expanded. UI blindly obeys the return value.
		/// There is no way the node will be expanded in the UI without this property being true
		/// in one of the tree versions.
		/// </summary>
		bool IsExpanded { get; }
		/// <summary>
		/// Determines if tree node is selected. UI blindly obeys the return value.
		/// There is no way the node will be selected in the UI without this property being true
		/// in one of the tree versions.
		/// </summary>
		bool IsSelected { get; }
	};

	public class EmptyTreeNode : ITreeNode
	{
		public static ITreeNode Instance { get; } = new EmptyTreeNode();

		string ITreeNode.Key => "";
		IReadOnlyList<ITreeNode> ITreeNode.Children => empty;
		bool ITreeNode.IsExpanded => true;
		bool ITreeNode.IsSelected => false;

		static readonly IReadOnlyList<ITreeNode> empty = new List<ITreeNode>().AsReadOnly();
	};
}