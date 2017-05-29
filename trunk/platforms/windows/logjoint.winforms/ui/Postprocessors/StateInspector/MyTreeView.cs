using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
	public class MyTreeView : TreeView
	{
		readonly HashSet<TreeNode> selectedNodes = new HashSet<TreeNode>();
		bool selectionHandlerSuspended;

		public MyTreeView()
		{
			base.DrawMode = TreeViewDrawMode.OwnerDrawText;
		}

		public IEnumerable<TreeNode> SelectedNodes 
		{ 
			get { return selectedNodes; }
			set
			{
				var newNodes = value;
				if (!selectedNodes.SetEquals(newNodes))
				{
					selectedNodes.Clear();
					foreach (var n in newNodes)
						selectedNodes.Add(n);
					Invalidate();
					selectionHandlerSuspended = true;
					this.SelectedNode = selectedNodes.FirstOrDefault();
					selectionHandlerSuspended = false;
					OnSelectedNodesChanged();
				}
			}
		}

		public new void EndUpdate()
		{
			// remove non existing anymore nodes from selected nodes collection
			bool selectedNodesChanged = selectedNodes.RemoveWhere(n => n.TreeView == null) > 0;

			// move selection to valid selected node if there is any left
			if (this.SelectedNode != null && this.SelectedNode.TreeView == null)
				this.SelectedNode = selectedNodes.FirstOrDefault();

			base.EndUpdate();
			if (selectedNodesChanged)
			{
				OnSelectedNodesChanged();
			}
		}

		public void ScrollSelectedNodesInView()
		{
			if (this.SelectedNode != null)
				this.SelectedNode.EnsureVisible();
		}

		public event EventHandler SelectedNodesChanged;

		protected unsafe override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case 8270:
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

		static Color GetNodeRowBackgroundColor(TreeNode node)
		{
			var rowColor = node.BackColor;
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

		protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
		{
			if (selectionHandlerSuspended)
				return;

			if (ModifierKeys == Keys.Control)
			{
				if (selectedNodes.Contains(e.Node))
				{
					e.Cancel = true;
					selectedNodes.Remove(e.Node);
					OnSelectedNodesChanged();
				}
				else
				{
					selectedNodes.Add(e.Node);
					OnSelectedNodesChanged();
				}
			}
			else
			{
				if (!(selectedNodes.Count == 1 && selectedNodes.First() == e.Node))
				{
					foreach (var n in selectedNodes)
						InvalidateNode(n);
					selectedNodes.Clear();
					selectedNodes.Add(e.Node);
					OnSelectedNodesChanged();
				}
			}

			if (!e.Cancel)
			{
				base.OnBeforeSelect(e);
			}
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
				this.SelectedNode = ht.Node;
		}

		protected override async void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			var node = this.GetNodeAt(e.Location);
			if (node == null)
				return;
			if (node == this.SelectedNode) // node is the selected treeview node?
			{
				// handle multiselecttion logic for this particular case.
				// node that clicking on the selected node does not raise OnBeforeSelect.

				if (ModifierKeys == Keys.Control)
				{
					await Task.Yield(); // let current click be handled normally (ignored). schedule the rest of method to UI thread.
					selectedNodes.Remove(node);
					selectionHandlerSuspended = true;
					this.SelectedNode = selectedNodes.FirstOrDefault();
					selectionHandlerSuspended = false;
					OnSelectedNodesChanged();
				}
				else
				{
					if (selectedNodes.Count >= 2) // clicking in selected node w/o ctrl
					{
						// ensure only one node is selected
						selectedNodes.Clear();
						selectedNodes.Add(node);
						Invalidate();
						OnSelectedNodesChanged();
					}
				}
			}
		}

		protected override void OnDrawNode(DrawTreeNodeEventArgs e)
		{
			if (e.Node == null)
				return;

			var selected = selectedNodes.Contains(e.Node);
			var focused = e.Node.TreeView.Focused;
			var treeSelected = this.SelectedNode == e.Node;
			var font = e.Node.NodeFont ?? e.Node.TreeView.Font;

			if (selected)
			{
				e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
				TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds, 
					System.Drawing.SystemColors.HighlightText, TextFormatFlags.Default);
			}
			else
			{
				using (var b = new SolidBrush(GetNodeRowBackgroundColor(e.Node)))
					e.Graphics.FillRectangle(b, e.Bounds);
				TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds,
					(e.Node.ForeColor != Color.Empty) ? e.Node.ForeColor : this.ForeColor, TextFormatFlags.Default);
			}

			base.OnDrawNode(e);

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

		void OnSelectedNodesChanged()
		{
			if (SelectedNodesChanged != null)
				SelectedNodesChanged(this, EventArgs.Empty);
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
}
