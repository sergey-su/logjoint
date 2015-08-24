using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI.DebugOutput
{
	public partial class DebugOutputFactoryUI : UserControl, ILogProviderUI
	{
		public DebugOutputFactoryUI()
		{
			InitializeComponent();
		}

		Control ILogProviderUI.UIControl
		{
			get { return this; }
		}

		void ILogProviderUI.Apply(IModel model)
		{
			model.CreateLogSource(LogJoint.DebugOutput.Factory.Instance, new ConnectionParams());
		}
	}
}
