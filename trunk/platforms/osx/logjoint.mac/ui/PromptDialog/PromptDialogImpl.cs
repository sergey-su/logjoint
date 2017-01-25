using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters
{
	public class PromptDialogImpl: IPromptDialog
	{
		string IPromptDialog.ExecuteDialog(string caption, string prompt, string defaultValue)
		{
			var ctrl = new PromptDialogController();
			ctrl.Window.Title = caption;
			
			
		}
	}
}
