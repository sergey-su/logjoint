using System;
using LogJoint.UI.Presenters.About;

namespace LogJoint.UI
{
	public class AboutDialogConfig: IAboutConfig
	{
		string IAboutConfig.WinInstallerUri
		{
			get { return LogJoint.Properties.Settings.Default.WinInstallerUrl; }
		}

		string IAboutConfig.MacInstallerUri
		{
			get { return LogJoint.Properties.Settings.Default.MacInstallerUrl; }
		}

		string IAboutConfig.FeedbackEMail
		{
			get { return LogJoint.Properties.Settings.Default.FeedbackEmail; }
		}
	}
}

