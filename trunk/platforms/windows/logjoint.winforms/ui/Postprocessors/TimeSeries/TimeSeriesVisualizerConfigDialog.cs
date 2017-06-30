using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using LJD = LogJoint.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesVisualizerConfigDialog : ToolForm, IConfigDialogView
	{
		readonly IConfigDialogEventsHandler evts;
		readonly Drawing.Resources resources;
		bool updateLocked;

		public TimeSeriesVisualizerConfigDialog(IConfigDialogEventsHandler evts, Drawing.Resources resources)
		{
			this.evts = evts;
			this.resources = resources;
			InitializeComponent();
		}

		void IConfigDialogView.AddRootNode(TreeNodeData n)
		{
			treeView.Nodes.Add(CreateNode(n));
		}

		void IConfigDialogView.RemoveRootNode(TreeNodeData n)
		{
			var tn = treeView.Nodes.OfType<TreeNode>().FirstOrDefault(x => x.Tag == n);
			if (tn != null)
				tn.Remove();
		}

		IEnumerable<TreeNodeData> IConfigDialogView.GetRoots()
		{
			return treeView.Nodes.OfType<TreeNode>().Select(x => x.Tag).OfType<TreeNodeData>();
		}

		void IConfigDialogView.UpdateNodePropertiesControls(NodeProperties props)
		{
			descriptionLabel.Text = props?.Description ?? "";
			updateLocked = true;
			if ((colorComboBox.Enabled = props != null && props.Color != null) == true)
			{
				if (colorComboBox.Items.Count == 0)
					colorComboBox.Items.AddRange(props.Palette.Select(c => (object)c).ToArray());
				colorComboBox.SelectedIndex = props.Palette.IndexOf(c => c.Argb == props.Color.Value.Argb).GetValueOrDefault(-1);
			}
			if ((markerComboBox.Enabled = props != null && props.Marker != null) == true)
			{
				if (markerComboBox.Items.Count == 0)
					markerComboBox.Items.AddRange(typeof(MarkerType).GetEnumValues().OfType<object>().ToArray());
				markerComboBox.SelectedIndex = typeof(MarkerType).GetEnumValues().OfType<MarkerType>().IndexOf(c => c == props.Marker.Value).GetValueOrDefault(-1);
			}
			updateLocked = false;
		}

		bool IConfigDialogView.Visible
		{
			get { return base.Visible; }
			set { base.Visible = value; }
		}

		TreeNodeData IConfigDialogView.SelectedNode
		{
			get { return treeView.SelectedNode?.Tag as TreeNodeData; }
			set
			{
				Func<TreeNodeCollection, bool> helper = null;
				helper = nodes =>
				{
					foreach (TreeNode n in nodes)
					{
						if (n.Tag == value)
						{
							treeView.SelectedNode = n;
							return true;
						}
						if (helper(n.Nodes))
							return true;
					}
					return false;
				};
				helper(treeView.Nodes);
			}
		}

		void IConfigDialogView.Activate()
		{
			this.Activate();
			if (treeView.CanFocus)
				treeView.Focus();
		}

		TreeNode CreateNode(TreeNodeData d, bool isTopLevel = true)
		{
			TreeNode n = d.Checkable ? new TreeNode() : new HiddenCheckBoxTreeNode();
			n.Text = d.Caption;
			n.Tag = d;
			if (d.Checkable)
				n.Checked = evts.IsNodeChecked(d);
			foreach (var c in d.Children)
				n.Nodes.Add(CreateNode(c, false));
			if (isTopLevel)
				n.Expand();
			return n;
		}

		private void uncheckAllLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			updateLocked = true;
			var nodes = new List<TreeNodeData>();
			Action<TreeNode> helper = null;
			helper = n =>
			{
				if (n.Checked)
				{
					nodes.Add(n.Tag as TreeNodeData);
					n.Checked = false;
				}
				foreach (TreeNode c in n.Nodes)
					helper(c);
			};
			foreach (TreeNode c in treeView.Nodes)
				helper(c);
			evts.OnNodesChecked(nodes.Where(n=> n != null), false);
			updateLocked = false;
		}

		private void collapseAllLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			treeView.CollapseAll();
		}

		private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (updateLocked)
				return;
			var d = e.Node.Tag as TreeNodeData;
			if (d != null && d.Checkable)
				evts.OnNodesChecked(new[] { d }, e.Node.Checked);
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			evts.OnSelectedNodeChanged();
		}

		private void colorComboBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0 || (e.State & DrawItemState.Disabled) != 0)
				return;
			e.DrawBackground();
			using (var sb = new SolidBrush(((ModelColor)colorComboBox.Items[e.Index]).ToColor()))
				e.Graphics.FillRectangle(sb, Rectangle.Inflate(e.Bounds, -5, -2));
		}

		private void colorComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updateLocked && colorComboBox.SelectedItem is ModelColor)
				evts.OnColorChanged((ModelColor)colorComboBox.SelectedItem);
		}

		private void markerComboBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0 || (e.State & DrawItemState.Disabled) != 0)
				return;
			e.DrawBackground();
			using (var g = new LJD.Graphics(e.Graphics))
			{
				Drawing.DrawLegendSample(
					g, resources,
					new ModelColor(Color.Blue.ToArgb()),
					(MarkerType)markerComboBox.Items[e.Index],
					Rectangle.Inflate(e.Bounds, -3, 0)
				);
			}
		}

		private void markerComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updateLocked && markerComboBox.SelectedItem is MarkerType)
				evts.OnMarkerChanged((MarkerType)markerComboBox.SelectedItem);
		}
	}
}
