using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchPanel;

namespace LogJoint.UI
{
	public partial class SearchPanelView : UserControl, IView
	{
		public SearchPanelView()
		{
			InitializeComponent();
		}

		public void SetPresenter(IPresenterEvents presenter)
		{
			this.presenter = presenter;
		}


		IPresenterEvents presenter;
	}
}
