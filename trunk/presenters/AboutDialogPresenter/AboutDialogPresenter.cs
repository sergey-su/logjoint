using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using LogJoint;
using LogJoint.UI;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.About
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IAboutConfig config;
		readonly IClipboardAccess clipboardAccess;

		public Presenter(
			IView view, 
			IAboutConfig config,
			IClipboardAccess clipboardAccess)
		{
			this.view = view;
			this.config = config;
			this.clipboardAccess = clipboardAccess;

			view.SetEventsHandler(this);
		}

		void IPresenter.Show()
		{
			var text = string.Format(
				"LogJoint{0}" +
				"Log viewer tool for professionals.{0}" +
				"Assembly version: {1}{0}" +
				"http://logjoint.codeplex.com/",
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
			if (!string.IsNullOrEmpty(config.FeedbackEMail))
			{
				feedbackLink = "mailto:" + config.FeedbackEMail;
			}

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
			if (!string.IsNullOrEmpty(config.FeedbackEMail))
			{
				string link = string.Format("mailto:{0}?subject=LogJoint feedback", config.FeedbackEMail);
				Process.Start(link); 
			}
		}
	};
};