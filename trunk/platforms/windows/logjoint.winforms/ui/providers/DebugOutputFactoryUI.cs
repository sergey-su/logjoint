using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.DebugOutput
{
	public partial class DebugOutputFactoryUI : UserControl, ILogProviderFactoryUI
	{
		public DebugOutputFactoryUI()
		{
			InitializeComponent();
		}

		#region ILogReaderFactoryUI Members

		public object UIControl
		{
			get { return this; }
		}

		public void Apply(IFactoryUICallback callback)
		{
			callback.AddNewProvider(new LogProvider(callback.CreateHost()));
		}

		#endregion
	}
}
