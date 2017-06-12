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

		void IConfigDialogView.UpdateNodePropertiesControls(NodePropertiesData props)
		{
			// todo
			throw new NotImplementedException();
		}

		bool IConfigDialogView.Visible
		{
			get { return base.Visible; }
			set { base.Visible = value; }
		}

		TreeNode CreateNode(TreeNodeData d, bool isTopLevel = true)
		{
			TreeNode n = d.Checkable ? new TreeNode() : new HiddenCheckBoxTreeNode();
			n.Text = string.Format("{0} ({1})", d.Caption, d.Counter);
			n.Tag = d;
			if (d.Checkable)
				n.Checked = evts.IsChecked(d);
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
				evts.OnChecked(d, e.Node.Checked);
		}
	}
}
