using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.IssueReportDialogPresenter
{
	public interface IPresenter
	{
		bool IsAvailable { get; }
		void ShowDialog();
	};
}