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
		readonly Windows.Reactive.IReactive reactive;
		IViewModel viewModel;
		Windows.Reactive.ITreeViewController<IObjectsTreeNode> treeViewController;
		Dictionary<System.Drawing.Color, NodeColoringResources> nodeColoringResourcesCache = new();

		public StateInspectorForm(Windows.Reactive.IReactive reactive)
		{
			InitializeComponent();

			this.reactive = reactive;
			this.treeViewController = reactive.CreateTreeViewController<IObjectsTreeNode>(objectsTreeView);
			this.objectsTreeView.Indent = UIUtils.Dpi.Scale(20, 120);
			this.splitContainer1.SplitterWidth = Math.Max(4, UIUtils.Dpi.Scale(4, 120));
			this.splitContainer3.SplitterDistance = UIUtils.Dpi.Scale(260, 120);
			this.ClientSize = new System.Drawing.Size(UIUtils.Dpi.Scale(800, 120), UIUtils.Dpi.Scale(500, 120));
			this.treeViewController.OnExpand = node => viewModel.OnExpandNode(node);
			this.treeViewController.OnCollapse = node => viewModel.OnCollapseNode(node);
			this.treeViewController.OnSelect = nodes => viewModel.OnSelect(nodes);

			selectedObjectStateHistoryControl.Header.ResizingStarted += (s, e) => splitContainer3.BeginSplitting();

		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;
			selectedObjectStateHistoryControl.Init(viewModel, reactive);
			propertiesDataGridView.Init(viewModel);

			var updateTree = Updaters.Create(
				() => viewModel.ObjectsTreeRoot,
				treeViewController.Update
			);

			var updateCurrentTime = Updaters.Create(
				() => viewModel.CurrentTimeLabelText,
				text => currentTimeLabel.Text = text
			);

			var updatePropertiesTable = Updaters.Create(
				() => viewModel.ObjectsProperties,
				properties => propertiesDataGridView.DataSource = properties // todo: preserve selection
			);

			var repaintTree = Updaters.Create(
				() => viewModel.PaintNode,
				_ => objectsTreeView.Invalidate()
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateTree();
				repaintTree();
				updateCurrentTime();
				updatePropertiesTable();
			});
		}


		void IView.Show()
		{
			this.Visible = true;
			this.BringToFront();
		}

		void IView.ScrollStateHistoryItemIntoView(int itemIndex)
		{
			// todo
		}


		NodeColoringResources GetNodeColoringResources(NodePaintInfo paintInfo)
		{
			switch (paintInfo.Coloring)
			{
				case NodeColoring.Alive:
					return NodeColoringResources.Alive;
				case NodeColoring.Deleted:
					return NodeColoringResources.Deleted;
				case NodeColoring.NotCreatedYet:
					return NodeColoringResources.NotCreatedYet;
				case NodeColoring.LogSource:
					var cl = Drawing.PrimitivesExtensions.ToSystemDrawingObject(paintInfo.LogSourceColor.Value);
					if (nodeColoringResourcesCache.TryGetValue(cl, out var res))
						return res;
					res = new NodeColoringResources(cl, new SolidBrush(cl), new Color(), NodeColoring.LogSource);
					nodeColoringResourcesCache[cl] = res;
					return res;
				default:
					return NodeColoringResources.NotCreatedYet;
			}
		}

		private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			propertiesDataGridView.Capture = false;
			viewModel.OnPropertiesRowDoubleClicked(e.RowIndex);
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
				viewModel.OnVisibleChanged(this.Visible);
		}

		private void objectsTreeView_NodeDisplayAttributes(object sender, Windows.TreeViewAttributesEventArgs e)
		{
			if (viewModel == null)
				return;

			var paintInfo = viewModel.PaintNode(treeViewController.Map(e.Node), false);

			if (paintInfo.DrawingEnabled)
				e.BackColor = GetNodeColoringResources(paintInfo).BkColor;
		}

		private void objectsTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			if (viewModel == null)
				return;

			int spaceAvailableForDefaultPropValue = objectsTreeView.ClientSize.Width - e.Node.Bounds.Right;
			var paintInfo = viewModel.PaintNode(treeViewController.Map(e.Node), spaceAvailableForDefaultPropValue > 30);

			if (!paintInfo.DrawingEnabled)
				return;

			var coloringResources = GetNodeColoringResources(paintInfo);

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
				viewModel.OnNodeDeleteKeyPressed();
			}
		}

		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var menuData = viewModel.OnNodeMenuOpening();
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
