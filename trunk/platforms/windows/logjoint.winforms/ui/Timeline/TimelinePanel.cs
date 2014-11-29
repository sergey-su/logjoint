using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelinePanel : UserControl, IView
	{
		public TimelinePanel()
		{
			InitializeComponent();
		}

		public TimeLineControl TimelineControl { get { return timeLineControl; } }

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
			this.timelineToolBox.SetPresenter(presenter);
		}

		void IView.SetViewTailModeToolButtonState(bool buttonChecked)
		{
			this.timelineToolBox.viewTailModeToolStripButton.Checked = buttonChecked;
		}

		IViewEvents presenter;
	}
}
