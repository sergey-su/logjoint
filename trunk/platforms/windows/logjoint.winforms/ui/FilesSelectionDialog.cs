using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class FilesSelectionDialog : Form
	{
		public FilesSelectionDialog()
		{
			InitializeComponent();
		}

		public bool[] Execute(string prompt, string[] items)
		{
			this.Text = prompt;
			checkedListBox1.Items.Clear();
			foreach (string f in items)
				checkedListBox1.Items.Add(f, true);
			bool[] ret = new bool[items.Length];
			if (ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return ret;
			foreach (int idx in checkedListBox1.CheckedIndices)
				ret[idx] = true;
			return ret;
		}
	}
}
