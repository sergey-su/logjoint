using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.About
{
	public interface IPresenter
	{
		void Show();
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		void Show (
			string text, 
			string feedbackText,
			string feedbackLink,
			string shareText,
			string shareTextWin,
			string winInstallerLink,
			string shareTextMac,
			string macInstallerLink
		);
		void SetAutoUpdateControlsState (
			bool featureEnabled, bool checkNowEnabled,
			string status, string details
		);
	};

	public interface IViewEvents
	{
		void OnCopyWinInstallerLink();
		void OnCopyMacInstallerLink();
		void OnFeedbackLinkClicked();
		void OnUpdateNowClicked();
	};

	public interface IAboutConfig
	{
		string WinInstallerUri { get; }
		string MacInstallerUri { get; }
		string FeedbackUrl { get; }
	};
};