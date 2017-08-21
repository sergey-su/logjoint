using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using LogJoint;
using LogJoint.UI;
using System.Diagnostics;
using LogJoint.AutoUpdate;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.About
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IAboutConfig config;
		readonly IClipboardAccess clipboardAccess;
		readonly AutoUpdate.IAutoUpdater autoUpdater;

		public Presenter(
			IView view, 
			IAboutConfig config,
			IClipboardAccess clipboardAccess,
			AutoUpdate.IAutoUpdater autoUpdater
		)
		{
			this.view = view;
			this.config = config;
			this.clipboardAccess = clipboardAccess;
			this.autoUpdater = autoUpdater;

			view.SetEventsHandler(this);

			autoUpdater.Changed += (s, e) => 
			{
				UpdateAutoUpdateControls();
			};
		}

		void IPresenter.Show()
		{
			var text = string.Format(
				"LogJoint{0}" +
				"Log viewer tool for professionals.{0}" +
				"Assembly version: {1}{0}" +
				"https://github.com/sergey-su/logjoint",
				Environment.NewLine,
				Assembly.GetExecutingAssembly().GetName().Version
			);

			string win = config.WinInstallerUri;
			string mac = config.MacInstallerUri;

			if (string.IsNullOrEmpty (win))
				win = null;
			if (string.IsNullOrEmpty (mac))
				mac = null;

			string shareText = null;
			if (win != null || mac != null)
			{
				shareText = "Share the tool with other professionals";
			}

			string feedbackLink = null;
			if (!string.IsNullOrEmpty(config.FeedbackUrl))
			{
				feedbackLink = config.FeedbackUrl;
			}
				
			ScheduleUpdateAutoUpdateControls();
			view.Show(
				text,
				"Send feedback:",
				feedbackLink,
				shareText,
				"Win",
				win,
				"Mac",
				mac
			);
		}

		void IViewEvents.OnCopyWinInstallerLink()
		{
			if (!string.IsNullOrEmpty (config.WinInstallerUri)) 
			{
				clipboardAccess.SetClipboard (config.WinInstallerUri);
			}
		}

		void IViewEvents.OnCopyMacInstallerLink()
		{
			if (!string.IsNullOrEmpty (config.MacInstallerUri)) 
			{
				clipboardAccess.SetClipboard (config.MacInstallerUri);
			}
		}

		void IViewEvents.OnFeedbackLinkClicked()
		{
			if (!string.IsNullOrEmpty(config.FeedbackUrl))
			{
				string link = config.FeedbackUrl;
				if (link.StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase))
					link = string.Format("{0}?subject=LogJoint feedback", link);
				Process.Start(link); 
			}
		}

		void IViewEvents.OnUpdateNowClicked()
		{
			autoUpdater.CheckNow();
		}

		async void ScheduleUpdateAutoUpdateControls()
		{
			await Task.Yield();
			UpdateAutoUpdateControls();
		}

		void UpdateAutoUpdateControls()
		{
			var pres = autoUpdater.GetPresentation(preferShortBrief: true);
			view.SetAutoUpdateControlsState (
				pres.Enabled,
				pres.CanCheckNow,
				pres.Brief,
				pres.Details
			);
		}
	};
};