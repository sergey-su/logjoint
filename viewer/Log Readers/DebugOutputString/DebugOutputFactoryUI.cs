using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.DebugOutput
{
	public partial class DebugOutputFactoryUI : UserControl, ILogReaderFactoryUI
	{
		public DebugOutputFactoryUI()
		{
			InitializeComponent();
		}

		#region ILogReaderFactoryUI Members

		public Control UIControl
		{
			get { return this; }
		}

		public void Apply(IFactoryUICallback callback)
		{
			callback.AddNewReader(new LogReader(callback.CreateHost()));
		}

		#endregion
	}
}
