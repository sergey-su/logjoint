using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Windows.Reactive
{
    class TreeViewController<Node> : ITreeViewController<Node> where Node : class, ITreeNode
    {
        readonly MultiselectTreeView treeView;
        readonly Dictionary<ITreeNode, ViewNode> nodeToViewNodes = new Dictionary<ITreeNode, ViewNode>();
        ITreeNode currentRoot = EmptyTreeNode.Instance;
        bool updating;

        public TreeViewController(MultiselectTreeView treeView)
        {
            this.treeView = treeView;
            treeView.BeforeExpand += (s, e) =>
            {
                if (updating)
                    return;
                e.Cancel = true;
                OnExpand?.Invoke(Map(e.Node));
            };
            treeView.BeforeCollapse += (s, e) =>
            {
                if (updating)
                    return;
                e.Cancel = true;
                OnCollapse?.Invoke(Map(e.Node));
            };
            treeView.BeforeMultiSelect += (s, e) =>
            {
                if (updating)
                    return;
                e.Cancel = true;
                OnSelect?.Invoke(e.Nodes.OfType<ViewNode>().Select(n => n.Node).ToArray());
            };
        }

        public Action<Node[]> OnSelect { get; set; }
        public Action<Node> OnExpand { get; set; }
        public Action<Node> OnCollapse { get; set; }
        public Action<TreeNode, Node, Node> OnUpdateNode { get; set; }

        public Node Map(TreeNode node)
        {
            return (node as ViewNode)?.Node;
        }

        public TreeNode Map(Node node)
        {
            nodeToViewNodes.TryGetValue(node, out var vn);
            return vn;
        }

        public void Update(Node newRoot)
        {
            var finalizeActions = new List<Action>();

            bool updateBegun = false;
            Action beginUpdate = () =>
            {
                if (!updateBegun)
                {
                    // Call ListView's BeginUpdate/EndUpdate only when needed
                    // tree structure changed to avoid flickering when only selection changes.
                    treeView.BeginUpdate();
                    updateBegun = true;
                }
            };

            updating = true;
            try
            {
                var edits = TreeEdit.GetTreeEdits(currentRoot, newRoot);
                currentRoot = newRoot;

                foreach (var e in edits)
                {
                    TreeNode node = e.Node == newRoot ? null : nodeToViewNodes[e.Node];
                    TreeNodeCollection nodeChildren = e.Node == newRoot ? treeView.Nodes : node.Nodes;
                    switch (e.Type)
                    {
                        case TreeEdit.EditType.Insert:
                            beginUpdate();
                            var insertedNode = CreateViewNode((Node)e.NewChild);
                            nodeChildren.Insert(e.ChildIndex, insertedNode);
                            break;
                        case TreeEdit.EditType.Delete:
                            beginUpdate();
                            var deletedNode = nodeChildren[e.ChildIndex];
                            nodeChildren.RemoveAt(e.ChildIndex);
                            Debug.Assert(deletedNode == nodeToViewNodes[e.OldChild]);
                            nodeToViewNodes.Remove(e.OldChild);
                            DeleteDescendantsFromMap(deletedNode);
                            break;
                        case TreeEdit.EditType.Reuse:
                            var nodeToReuse = nodeToViewNodes[e.OldChild];
                            Rebind(nodeToReuse, (Node)e.NewChild);
                            UpdateViewNode(nodeToReuse, (Node)e.NewChild, (Node)e.OldChild, beginUpdate);
                            break;
                        case TreeEdit.EditType.Expand:
                            finalizeActions.Add(node.Expand);
                            break;
                        case TreeEdit.EditType.Collapse:
                            finalizeActions.Add(() => node.Collapse(ignoreChildren: true));
                            break;
                        case TreeEdit.EditType.Select:
                            treeView.SelectNode(node);
                            break;
                        case TreeEdit.EditType.Deselect:
                            treeView.DeselectNode(node);
                            break;
                    }
                }
            }
            finally
            {
                if (updateBegun)
                {
                    treeView.EndUpdate();
                }
                finalizeActions.ForEach(a => a());
                if (treeView.SelectedNode is ViewNode selected && !selected.Node.IsSelected)
                {
                    // When no nodes are explicitly selected, the OS selects one for us,
                    // and we have to undo that.
                    // Another scenario is that a node that was optimistically allowed to be selected in BeforeSelect
                    // turned out not be selected.
                    treeView.SelectedNode = null;
                }
                updating = false;
            }
        }

        ViewNode CreateViewNode(Node node)
        {
            var result = new ViewNode { Node = node };
            UpdateViewNode(result, node, null, null);
            nodeToViewNodes.Add(node, result);
            return result;
        }

        void Rebind(ViewNode viewNode, Node newNode)
        {
            nodeToViewNodes.Remove(viewNode.Node);
            viewNode.Node = newNode;
            nodeToViewNodes[newNode] = viewNode;
        }

        void DeleteDescendantsFromMap(TreeNode item)
        {
            foreach (ViewNode c in item.Nodes)
            {
                Debug.Assert(nodeToViewNodes.Remove(c.Node));
                DeleteDescendantsFromMap(c);
            }
        }

        void UpdateViewNode(TreeNode viewNode, Node newNode, Node oldNode, Action beginUpdate)
        {
            if (OnUpdateNode != null)
            {
                OnUpdateNode(viewNode, newNode, oldNode);
            }
            else
            {
                var newText = newNode.ToString();
                if (oldNode == null || newText != oldNode.ToString())
                {
                    beginUpdate?.Invoke();
                    viewNode.Text = newText;
                }
            }
        }

        class ViewNode : TreeNode
        {
            internal Node Node;
        };
    }
}
