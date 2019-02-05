using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class AllTagsDialog : Form
	{
		public AllTagsDialog()
		{
			InitializeComponent();
		}

		public HashSet<string> SelectTags(IEnumerable<KeyValuePair<string, bool>> tags, string focusedTag)
		{
			checkedListBox1.BeginUpdate();
			int focusedTagIndex = -1;
			foreach (var t in tags)
			{
				var idx = checkedListBox1.Items.Add(t.Key);
				if (t.Value)
					checkedListBox1.SetItemChecked(idx, true);
				if (focusedTag != null && t.Key == focusedTag)
					focusedTagIndex = idx;
			}
			checkedListBox1.EndUpdate();
			if (focusedTagIndex >= 0)
			{
				checkedListBox1.SelectedIndex = focusedTagIndex;
				checkedListBox1.TopIndex = focusedTagIndex;
			}
			if (ShowDialog() == DialogResult.Cancel)
				return null;
			return new HashSet<string>(checkedListBox1.CheckedItems.OfType<string>());
		}

		private void checkAllLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			for (var i = 0; i < checkedListBox1.Items.Count; ++i)
				checkedListBox1.SetItemChecked(i, sender == checkAllLinkLabel);
		}
	}
}
