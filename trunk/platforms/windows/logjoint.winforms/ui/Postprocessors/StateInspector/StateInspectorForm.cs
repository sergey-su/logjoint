using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.ComponentModel;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class StateInspectorForm : ToolForm, IView
	{
		IViewModel viewModel;
		bool expandingProgrammatically;

		public StateInspectorForm()
		{
			InitializeComponent();

			this.objectsTreeView.Indent = UIUtils.Dpi.Scale(20, 120);
			this.splitContainer1.SplitterWidth = Math.Max(4, UIUtils.Dpi.Scale(4, 120));
			this.splitContainer3.SplitterDistance = UIUtils.Dpi.Scale(260, 120);
			this.ClientSize = new System.Drawing.Size(UIUtils.Dpi.Scale(800, 120), UIUtils.Dpi.Scale(500, 120));
			this.objectsTreeView.BeforeExpand += (s, e) => { if (!expandingProgrammatically) viewModel.OnNodeExpanding(GetNodeInternal(e.Node)); };

			selectedObjectStateHistoryControl.Header.ResizingStarted += (s, e) => splitContainer3.BeginSplitting();

		}

		void IView.SetEventsHandler(IViewModel viewModel)
		{
			this.viewModel = viewModel;
			selectedObjectStateHistoryControl.Init(viewModel);
			propertiesDataGridView.Init(viewModel);
		}

		NodesCollectionInfo IView.RootNodesCollection
		{
			get { return GetNodesCollection(objectsTreeView.Nodes); }
		}

		void IView.Clear(NodesCollectionInfo nodesCollection)
		{
			((TreeNodeCollection)nodesCollection.Data).Clear();
		}

		void IView.AddNode(NodesCollectionInfo nodesCollection, NodeInfo node)
		{
			((TreeNodeCollection)nodesCollection.Data).Add((TreeNode)node.Data);
		}

		NodeInfo[] IView.SelectedNodes
		{
			get { return objectsTreeView.SelectedNodes.Select(n => GetNodeInternal(n)).ToArray(); }
			set { objectsTreeView.SelectedNodes = value.Select(n => (TreeNode)n.Data).ToArray(); }
		}

		void IView.SetNodeText(NodeInfo node, string text)
		{
			((TreeNode)node.Data).Text = text;
		}

		void IView.ScrollSelectedNodesInView()
		{
			objectsTreeView.ScrollSelectedNodesInView();
		}

		void IView.BeginTreeUpdate()
		{
			objectsTreeView.BeginUpdate();
		}

		void IView.EndTreeUpdate()
		{
			objectsTreeView.EndUpdate();
		}

		void IView.InvalidateTree()
		{
			objectsTreeView.Invalidate();
		}

		bool IView.TreeSupportsLoadingOnExpansion
		{
			get { return true; }
		}
		NodeInfo IView.CreateNode(string nodeText, object tag, NodesCollectionInfo nodesCollection)
		{
			var viewNode = new TreeNode(nodeText) { Tag = tag };
			if (nodesCollection.Data != null)
				((TreeNodeCollection)nodesCollection.Data).Add(viewNode);
			return GetNodeInternal(viewNode);
		}

		async void IView.ExpandAll(NodeInfo node)
		{
			expandingProgrammatically = true;
			((TreeNode)node.Data).ExpandAll();
			await Task.Yield();
			expandingProgrammatically = false;
		}

		void IView.Collapse(NodeInfo node)
		{
			((TreeNode)node.Data).Collapse();
		}

		IEnumerable<NodeInfo> IView.EnumCollection(NodesCollectionInfo nodesCollection)
		{
			return ((TreeNodeCollection)nodesCollection.Data).Cast<TreeNode>().Select(GetNodeInternal);
		}

		int? IView.SelectedPropertiesRow
		{
			get
			{
				if (propertiesDataGridView.SelectedRows.Count > 0)
					return propertiesDataGridView.SelectedRows[0].Index;
				return null;
			}
			set
			{
				if (value != null)
					propertiesDataGridView.Rows[value.Value].Selected = true;
			}
		}

		void IView.SetPropertiesDataSource(IList<KeyValuePair<string, object>> properties)
		{
			propertiesDataGridView.DataSource = properties;
		}

		void IView.SetCurrentTimeLabelText(string text)
		{
			currentTimeLabel.Text = text;
		}

		void IView.Show()
		{
			this.Visible = true;
			this.BringToFront();
		}

		void IView.SetNodeColoring(NodeInfo nodeObj, NodeColoring coloring)
		{
			var node = (TreeNode)nodeObj.Data;
			var res = GetNodeColoringResources(coloring);
			node.BackColor = res.BkColor;
			node.ForeColor = res.FontColor;
		}

		IEnumerable<StateHistoryItem> IView.SelectedStateHistoryEvents
		{
			get { return selectedObjectStateHistoryControl.GetSelection(); }
		}

		void IView.BeginUpdateStateHistoryList(bool fullUpdate, bool clearList)
		{
			selectedObjectStateHistoryControl.BeginUpdate(fullUpdate, clearList);
		}

		int IView.AddStateHistoryItem(StateHistoryItem item)
		{
			return selectedObjectStateHistoryControl.AddItem(item);
		}

		void IView.EndUpdateStateHistoryList(int[] newSelectedIndexes, bool fullUpdate, bool redrawFocusedMessageMark)
		{
			selectedObjectStateHistoryControl.EndUpdate(newSelectedIndexes, fullUpdate, redrawFocusedMessageMark);
		}

		void IView.ScrollStateHistoryItemIntoView(int itemIndex)
		{
			// todo
		}


		NodeColoringResources GetNodeColoringResources(NodeColoring coloring)
		{
			switch (coloring)
			{
				case NodeColoring.Alive:
					return NodeColoringResources.Alive;
				case NodeColoring.Deleted:
					return NodeColoringResources.Deleted;
				case NodeColoring.NotCreatedYet:
					return NodeColoringResources.NotCreatedYet;
				default:
					return NodeColoringResources.NotCreatedYet;
			}
		}

		static NodeColoringResources GetNodeColoringResources(TreeNode node)
		{
			if (node.BackColor == NodeColoringResources.Alive.BkColor)
				return NodeColoringResources.Alive;
			if (node.BackColor == NodeColoringResources.Deleted.BkColor)
				return NodeColoringResources.Deleted;
			return NodeColoringResources.NotCreatedYet;
		}

		void objectsTreeView_SelectedNodesChanged(object sender, EventArgs e)
		{
			viewModel.OnSelectedNodesChanged();
		}

		private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			propertiesDataGridView.Capture = false;
			viewModel.OnPropertiesRowDoubleClicked();
		}

		private void propertiesDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			foreach (DataGridViewRow r in propertiesDataGridView.Rows)
			{
				var paintInfo = viewModel.OnPropertyCellPaint(r.Index);
				if (paintInfo.PaintAsLink)
				{
					r.Cells[1] = new DataGridViewLinkCell();
					var linkCell = r.Cells[1] as DataGridViewLinkCell;
					linkCell.LinkColor = Color.BlueViolet;
					linkCell.TrackVisitedState = false;
				}
				if (paintInfo.AddLeftPadding)
				{
					r.Cells[0].Style.Padding = new Padding(15, 0, 0, 0);
				}
			}
		}

		private void propertiesDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 1)
				viewModel.OnPropertyCellClicked(e.RowIndex);
		}

		private void CallObjectsForm_VisibleChanged(object sender, EventArgs e)
		{
			if (viewModel != null)
				viewModel.OnVisibleChanged();
		}

		private void objectsTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			if (viewModel == null)
				return;


			int spaceAvailableForDefaultPropValue = objectsTreeView.ClientSize.Width - e.Node.Bounds.Right;
			var paintInfo = viewModel.OnPaintNode(GetNodeInternal(e.Node), spaceAvailableForDefaultPropValue > 30);

			if (!paintInfo.DrawingEnabled)
				return;

			var coloringResources = GetNodeColoringResources(paintInfo.Coloring);

			if (paintInfo.PrimaryPropValue != null)
			{
				Rectangle r = new Rectangle(
					e.Node.Bounds.Right,
					e.Bounds.Y,
					spaceAvailableForDefaultPropValue,
					e.Bounds.Height);
				TextRenderer.DrawText(e.Graphics, paintInfo.PrimaryPropValue, objectsTreeView.Font, r, Color.Gray, coloringResources.BkColor,
					TextFormatFlags.SingleLine | TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			}

			if (paintInfo.DrawFocusedMsgMark)
			{
				var img = StateInspectorResources.FocusedMsgSlave;
				var sz = img.GetSize(width: UIUtils.Dpi.Scale(7, 192));
				e.Graphics.DrawImage(img, new RectangleF(
					1, e.Bounds.Top + (e.Bounds.Height - sz.Height) / 2, sz.Width, sz.Height));
			}
		}

		void objectsTreeView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				viewModel.OnDeleteKeyPressed();
			}
		}

		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var menuData = viewModel.OnMenuOpening();
			if (menuData.Items.Count == 0)
			{
				e.Cancel = true;
			}
			else
			{
				e.Cancel = false;
				var old = contextMenuStrip.Items.OfType<ToolStripMenuItem>().ToList();
				if (old.Count > 0)
				{
					contextMenuStrip.Items.Clear();
					old.ForEach(i => i.Dispose());
				}
				foreach (var i in menuData.Items)
				{
					var menuItem = new ToolStripMenuItem(i.Text);
					menuItem.Click += (clickSender, clickArgs) => i.Click();
					contextMenuStrip.Items.Add(menuItem);
				}
			}
		}

		static NodeInfo GetNodeInternal(TreeNode node)
		{
			if (node == null)
				return new NodeInfo();
			return new NodeInfo()
			{
				Data = node,
				Tag = node.Tag,
				ChildrenNodesCollection = GetNodesCollection(node.Nodes),
				Text = node.Text,
				Coloring = GetNodeColoringResources(node).Code
			};
		}

		static NodesCollectionInfo GetNodesCollection(TreeNodeCollection coll)
		{
			return new NodesCollectionInfo()
			{
				Data = coll
			};
		}

		struct NodeColoringResources
		{
			public NodeColoring Code;
			public Color BkColor;
			public Brush BkBrush;
			public Color FontColor;

			public NodeColoringResources(Color bkColor, Brush bkBrush, Color fontColor, NodeColoring code)
			{
				BkColor = bkColor;
				BkBrush = bkBrush;
				FontColor = fontColor;
				Code = code;
			}

			readonly static Color myGreen = Color.FromArgb(220, 255, 220);
			readonly static Color myGray = Color.FromArgb(240, 240, 255);

			public static readonly NodeColoringResources Alive = new NodeColoringResources(myGreen, new SolidBrush(myGreen), new Color(), NodeColoring.Alive);
			public static readonly NodeColoringResources Deleted = new NodeColoringResources(Color.Lavender, new SolidBrush(myGray), Color.Gray, NodeColoring.Deleted);
			public static readonly NodeColoringResources NotCreatedYet = new NodeColoringResources(new Color(), Brushes.White, new Color(), NodeColoring.NotCreatedYet);
		};
	}
}
