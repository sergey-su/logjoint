using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Immutable;

namespace LogJoint.UI.Windows
{
	public class MultiselectTreeView : TreeView
	{
		ImmutableHashSet<TreeNode> selectedNodes = ImmutableHashSet.Create<TreeNode>();
		bool selectionHandlerSuspended;
		TreeNode primarySelectionCandidate1, primarySelectionCandidate2;

		public MultiselectTreeView()
		{
			base.DrawMode = TreeViewDrawMode.OwnerDrawText;
		}

		public IReadOnlyCollection<TreeNode> SelectedNodes
		{ 
			get { return selectedNodes; }
			set
			{
				var newNodes = ImmutableHashSet.CreateRange(value);
				if (!selectedNodes.SetEquals(newNodes))
				{
					TryNewMultiSelection(newNodes, null);
				}
			}
		}

		public void SelectNode(TreeNode node)
		{
			TryNewMultiSelection(selectedNodes.Add(node), null);
		}

		public void DeselectNode(TreeNode node)
		{
			TryNewMultiSelection(selectedNodes.Remove(node), null);
		}

		public new void EndUpdate()
		{
			// remove non existing anymore nodes from selected nodes collection
			var toRemove = selectedNodes.Where(n => n.TreeView == null).ToArray();

			selectedNodes = selectedNodes.Except(toRemove);

			// move selection to valid selected node if there is any left
			if (this.SelectedNode != null && this.SelectedNode.TreeView == null)
				this.SelectedNode = GetPrimarySelectedNode(selectedNodes);

			base.EndUpdate();
			if (toRemove.Length > 0)
			{
				OnSelectedNodesChanged();
			}
		}

		public void ScrollSelectedNodesInView()
		{
			if (this.SelectedNode != null)
				this.SelectedNode.EnsureVisible();
		}

		public event TreeViewMultiNodeCancelEventHandler BeforeMultiSelect;
		public event EventHandler AfterMultiSelect;
		public event TreeViewNodeDisplayAttributesEventHandler NodeDisplayAttributes;

		protected unsafe override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case OCM_NOTIFY:
					NMHDR* ptr = (NMHDR*)((void*)m.LParam);
					if (ptr->code == NM_CUSTOMDRAW)
					{
						NMTVCUSTOMDRAW nMTVCUSTOMDRAW = (NMTVCUSTOMDRAW)m.GetLParam(typeof(NMTVCUSTOMDRAW));
						int dwDrawStage = nMTVCUSTOMDRAW.nmcd.dwDrawStage;
						if (dwDrawStage == (CDDS_ITEM | CDDS_PREPAINT))
						{
							TreeNode treeNode = TreeNode.FromHandle(this, nMTVCUSTOMDRAW.nmcd.dwItemSpec);
							if (treeNode != null)
							{
								using (Graphics graphics = Graphics.FromHdcInternal(nMTVCUSTOMDRAW.nmcd.hdc))
								{
									var bounds = treeNode.Bounds;
									using (var brush = new SolidBrush(GetNodeRowBackgroundColor(treeNode)))
									{
										graphics.FillRectangle(brush, new Rectangle(0, bounds.Top, this.Width, bounds.Height));
									}
								}
							}
						}
					}
					break;
				case WM_ERASEBKGND:
					// in this handler erase only part of background that is not covered by nodes
					TreeNode lastVisible = FindLastVisible(Nodes);
					Rectangle r;
					if (lastVisible != null)
						r = new Rectangle(0, lastVisible.Bounds.Bottom, Width, Height);
					else
						r = new Rectangle(0, 0, Width, Height);
					using (var g = CreateGraphics())
						g.FillRectangle(Brushes.White, r);
					m.Result = (IntPtr)1;
					return; // skip default processing
				case WM_HSCROLL:
					Invalidate();
					break;
			}
			base.WndProc(ref m);
		}

		Color GetNodeRowBackgroundColor(TreeNode node)
		{
			var rowColor = node.BackColor;
			if (NodeDisplayAttributes != null)
			{
				var args = new TreeViewAttributesEventArgs(node);
				NodeDisplayAttributes.Invoke(this, args);
				rowColor = args.BackColor;
			}
			if (rowColor == Color.Empty)
				rowColor = Color.White;
			return rowColor;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			InvalidateIfMultiselected();
			base.OnLostFocus(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			InvalidateIfMultiselected();
			base.OnGotFocus(e);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			Invalidate();
			base.OnSizeChanged(e);
		}

		protected override void OnAfterCollapse(TreeViewEventArgs e)
		{
			Invalidate();
			base.OnAfterCollapse(e);
		}

		protected override void OnAfterExpand(TreeViewEventArgs e)
		{
			Invalidate();
			base.OnAfterExpand(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			this.Invalidate();
			base.OnMouseWheel(e);
		}

		bool TryNewMultiSelection(ImmutableHashSet<TreeNode> value, TreeNode primarySelectionCandidate)
		{
			if (primarySelectionCandidate != null)
			{
				primarySelectionCandidate2 = primarySelectionCandidate1;
				primarySelectionCandidate1 = primarySelectionCandidate;
			}
			if (!OnBeforeSelectedNodesChange(value))
			{
				return false;
			}
			selectedNodes = value;
			selectionHandlerSuspended = true;
			this.SelectedNode = GetPrimarySelectedNode(selectedNodes);
			selectionHandlerSuspended = false;
			Invalidate();
			OnSelectedNodesChanged();
			return true;
		}

		TreeNode GetPrimarySelectedNode(ImmutableHashSet<TreeNode> nodesSet)
		{
			TreeNode node;
			if (nodesSet.Contains(primarySelectionCandidate1))
				node = primarySelectionCandidate1;
			else if (nodesSet.Contains(primarySelectionCandidate2))
				node = primarySelectionCandidate2;
			else
				node = nodesSet.FirstOrDefault();
			return node;
		}

		protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
		{
			if (selectionHandlerSuspended)
				return;

			bool tryNewMultiSelection(ImmutableHashSet<TreeNode> value, TreeNode primarySelectionCandidate)
			{
				if (!TryNewMultiSelection(value, primarySelectionCandidate))
				{
					e.Cancel = true;
					return false;
				}
				return true;
			}

			if (ModifierKeys == Keys.Control)
			{
				if (selectedNodes.Contains(e.Node))
				{
					if (tryNewMultiSelection(selectedNodes.Remove(e.Node), null))
					{
						e.Cancel = true;
					}
				}
				else
				{
					tryNewMultiSelection(selectedNodes.Add(e.Node), e.Node);
				}
			}
			else
			{
				var savedSel = selectedNodes;
				if (tryNewMultiSelection(ImmutableHashSet.Create(e.Node), e.Node))
				{
					foreach (var n in savedSel)
						InvalidateNode(n);
				}
			}
		}

		protected override void OnAfterSelect(TreeViewEventArgs e)
		{
			// ignore. This control does not raise AfterSelect event.
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			var ht = this.HitTest(e.Location);
			bool selectNode = false;
			if (ht.Node != null && e.Button == MouseButtons.Left &&
					(ht.Location == TreeViewHitTestLocations.RightOfLabel || ht.Location == TreeViewHitTestLocations.Indent))
			{
				selectNode = true;
			}
			if (ht.Node != null && e.Button == MouseButtons.Right &&
					(ht.Location == TreeViewHitTestLocations.Label || ht.Location == TreeViewHitTestLocations.RightOfLabel || ht.Location == TreeViewHitTestLocations.Indent))
			{
				selectNode = true;
			}
			if (selectNode)
				TryNewMultiSelection(ImmutableHashSet.Create(ht.Node), ht.Node);
		}

		protected override async void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			var node = this.GetNodeAt(e.Location);
			if (node == null)
				return;
			if (node == this.SelectedNode) // node is the selected treeview node?
			{
				// handle multi-selection logic for this particular case.
				// node that clicking on the selected node does not raise OnBeforeSelect.

				if (ModifierKeys == Keys.Control)
				{
					await Task.Yield(); // let current click be handled normally (ignored). schedule the rest of method to UI thread.
					TryNewMultiSelection(selectedNodes.Remove(node), null);
				}
				else
				{
					if (selectedNodes.Count >= 2) // clicking in selected node w/o ctrl
					{
						// ensure only one node is selected
						TryNewMultiSelection(ImmutableHashSet.Create(node), node);
					}
				}
			}
		}

		protected override void OnDrawNode(DrawTreeNodeEventArgs e)
		{
			if (e.Node == null)
				return;

			e.DrawDefault = true;

			base.OnDrawNode(e);

			if (e.DrawDefault)
			{
				var selected = selectedNodes.Contains(e.Node);
				var focused = e.Node.TreeView.Focused;
				var font = e.Node.NodeFont ?? e.Node.TreeView.Font;
				var textBounds = e.Bounds;
				if (CheckBoxes)
				{
					var pad = 1;
					textBounds.X += pad;
					textBounds.Width -= pad;
				}

				if (selected)
				{
					e.Graphics.FillRectangle(SystemBrushes.Highlight, textBounds);
					TextRenderer.DrawText(e.Graphics, e.Node.Text, font, textBounds,
						SystemColors.HighlightText, TextFormatFlags.Default);
				}
				else
				{
					using (var b = new SolidBrush(GetNodeRowBackgroundColor(e.Node)))
						e.Graphics.FillRectangle(b, textBounds);
					TextRenderer.DrawText(e.Graphics, e.Node.Text, font, textBounds,
						(e.Node.ForeColor != Color.Empty) ? e.Node.ForeColor : this.ForeColor, TextFormatFlags.Default); // todo: use version with back color
				}
			}

			e.DrawDefault = false;
		}

		void InvalidateNode(TreeNode node)
		{
			var b = node.Bounds;
			this.Invalidate(new Rectangle(0, b.Top, this.Width, b.Height));
		}

		void InvalidateIfMultiselected()
		{
			if (selectedNodes.Count > 1)
				this.Invalidate();
		}

		bool OnBeforeSelectedNodesChange(ImmutableHashSet<TreeNode> nodes)
		{
			IReadOnlyCollection<TreeNode> orderedNodes;
			var primary = GetPrimarySelectedNode(nodes);
			if (primary != null)
				orderedNodes = Enumerable.Union(new[] { primary }, nodes.Remove(primary)).ToArray();
			else
				orderedNodes = nodes;
			var evt = new TreeViewMultiNodeCancelEventArgs(orderedNodes, false);
			BeforeMultiSelect?.Invoke(this, evt);
			return !evt.Cancel;
		}

		void OnSelectedNodesChanged()
		{
			AfterMultiSelect?.Invoke(this, EventArgs.Empty);
		}

		static TreeNode FindLastVisible(TreeNodeCollection nodes)
		{
			if (nodes.Count > 0)
			{
				var last = nodes[nodes.Count - 1];
				if (last.Nodes.Count > 0 && last.IsExpanded)
				{
					return FindLastVisible(last.Nodes);
				}
				else
				{
					return last;
				}
			}
			else
			{
				return null;
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
				return cp;
			}
		}


		const int WM_ERASEBKGND = 0x0014;
		const int WM_HSCROLL = 0x0114;
		const int NM_CUSTOMDRAW = -12;
		const int OCM_NOTIFY = 8270;
		const int CDDS_ITEM = 0x00010000;
		const int CDDS_PREPAINT = 0x00000001;
		const int CDDS_PREERASE = 0x00000003;
		const int CDDS_POSTERASE = 0x00000004;
		const int CDRF_NOTIFYITEMDRAW  = 0x00000020;

		#pragma warning disable 0649 // ignore unused fields in interop structures

		struct NMHDR
		{
			public IntPtr hwndFrom;
			public IntPtr idFrom;
			public int code;
		}

		[StructLayout(LayoutKind.Sequential)]
		class NMTVCUSTOMDRAW
		{
			public NMCUSTOMDRAW nmcd;
			public int clrText;
			public int clrTextBk;
			public int iLevel;
		}

		struct NMCUSTOMDRAW
		{
			public NMHDR nmcd;
			public int dwDrawStage;
			public IntPtr hdc;
			public RECT rc;
			public IntPtr dwItemSpec;
			public int uItemState;
			public IntPtr lItemlParam;
		}

		struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		#pragma warning restore 0649
	}

	public class TreeViewMultiNodeCancelEventArgs : CancelEventArgs
	{
		public TreeViewMultiNodeCancelEventArgs(IReadOnlyCollection<TreeNode> nodes, bool cancel): base(cancel)
		{
			Nodes = nodes;
		}
		public IReadOnlyCollection<TreeNode> Nodes { get; private set; }
	}

	public delegate void TreeViewMultiNodeCancelEventHandler(object sender, TreeViewMultiNodeCancelEventArgs e);

	public class TreeViewAttributesEventArgs : EventArgs
	{
		public TreeViewAttributesEventArgs(TreeNode node)
		{
			Node = node;
			BackColor = node.BackColor;
			ForeColor = node.ForeColor;
		}

		public TreeNode Node { get; private set; }
		public Color BackColor { get; set; }
		public Color ForeColor { get; set; }
	};

	public delegate void TreeViewNodeDisplayAttributesEventHandler(object sender, TreeViewAttributesEventArgs e);
}
