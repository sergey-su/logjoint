using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesVisualizerConfigDialog : ToolForm, IConfigDialogView
	{
		readonly IConfigDialogEventsHandler evts;

		public TimeSeriesVisualizerConfigDialog(IConfigDialogEventsHandler evts)
		{
			this.evts = evts;
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
			descriptionLabel.Text = props?.Caption ?? "";
			if ((colorComboBox.Enabled = props != null && props.Color != null) == true)
			{
				if (colorComboBox.Items.Count == 0)
					colorComboBox.Items.AddRange(props.Palette.Select(c => (object)c).ToArray());
				colorComboBox.SelectedIndex = props.Palette.IndexOf(c => c.Argb == props.Color.Value.Argb).GetValueOrDefault(-1);
			}
			if ((markerComboBox.Enabled = props != null && props.Color != null) == true)
			{
				if (markerComboBox.Items.Count == 0)
					markerComboBox.Items.AddRange(typeof(MarkerType).GetEnumValues().OfType<object>().ToArray());
				colorComboBox.SelectedIndex = typeof(MarkerType).GetEnumValues().OfType<MarkerType>().IndexOf(c => c == props.Marker).GetValueOrDefault(-1);
			}
		}

		bool IConfigDialogView.Visible
		{
			get { return base.Visible; }
			set { base.Visible = value; }
		}

		TreeNodeData IConfigDialogView.SelectedNode
		{
			get { return treeView.SelectedNode?.Tag as TreeNodeData; }
		}

		TreeNode CreateNode(TreeNodeData d, bool isTopLevel = true)
		{
			TreeNode n = d.Checkable ? new TreeNode() : new HiddenCheckBoxTreeNode();
			n.Text = string.Format("{0} ({1})", d.Caption, d.Counter);
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
			Action<TreeNode> helper = null;
			helper = n =>
			{
				if (n.Checked)
					n.Checked = false;
				foreach (TreeNode c in n.Nodes)
					helper(c);
			};
			foreach (TreeNode c in treeView.Nodes)
				helper(c);
		}

		private void collapseAllLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			treeView.CollapseAll();
		}

		private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			var d = e.Node.Tag as TreeNodeData;
			if (d != null && d.Checkable)
				evts.OnNodeChecked(d, e.Node.Checked);
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			evts.OnSelectedNodeChanged();
		}

		private void colorComboBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0 || (e.State & DrawItemState.Disabled) != 0)
				return;
			using (var sb = new SolidBrush(((ModelColor)colorComboBox.Items[e.Index]).ToColor()))
				e.Graphics.FillRectangle(sb, Rectangle.Inflate(e.Bounds, -5, -2));
		}

		private void colorComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (colorComboBox.SelectedItem is ModelColor)
				evts.OnColorChanged((ModelColor)colorComboBox.SelectedItem);
		}
	}
}
