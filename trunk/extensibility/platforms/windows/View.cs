using System;
using System.Collections.Generic;
using System.Text;
using MonoMac.AppKit;

namespace LogJoint.Extensibility
{
	public interface View: IView
	{
		public void RegisterToolForm(Form f)
		{
			IWinFormsComponentsInitializer intf = mainForm;
			intf.InitOwnedForm(f, false);
		}
		UI.ILogProviderUIsRegistry ILogJointApplication.LogProviderUIsRegistry { get { return logProviderUIsRegistry; } }

		UI.MainForm mainForm;
		UI.ILogProviderUIsRegistry logProviderUIsRegistry;
	};
}
