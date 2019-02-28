using System;
using Foundation;
using ObjCRuntime;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint
{
	public class CustomURLSchemaEventsHandler: NSObject
	{
		private static CustomURLSchemaEventsHandler instance = new CustomURLSchemaEventsHandler();
		private string passedUrl;
		private bool isLoaded;
		private AppLaunch.ICommandLineHandler commandLineHandler;
		private ISynchronizationContext invoke;

		public static CustomURLSchemaEventsHandler Instance { get { return instance; }}

		public void Register()
		{
			NSAppleEventManager.SharedAppleEventManager.SetEventHandler(
				this,
				new Selector(@"getUrl:withReplyEvent:"),
				AEEventClass.Internet,
				AEEventID.GetUrl);
		}

		public void Init(IPresenter mainWindowPresenter, AppLaunch.ICommandLineHandler commandLineHandler, 
			ISynchronizationContext invoke)
		{
			this.commandLineHandler = commandLineHandler;
			this.invoke = invoke;
			mainWindowPresenter.Loaded += (sender, e) => 
			{
				if (isLoaded)
					return;
				isLoaded = true;
				LoadPassedUrl();
			};
		}

		[Export ("getUrl:withReplyEvent:")]
		public void GetUrl(NSAppleEventDescriptor evt, NSAppleEventDescriptor withReplyEvent)
		{
			passedUrl = evt.ParamDescriptorForKeyword(FourCC("----")).StringValue;
			if (isLoaded)
			{
				invoke.Invoke(LoadPassedUrl);
			}
		}

		static uint FourCC(string s)
		{
			return (((uint)s [0]) << 24 | ((uint)s [1]) << 16 | ((uint)s [2]) << 8 | ((uint)s [3]));
		}

		void LoadPassedUrl()
		{
			if (!string.IsNullOrEmpty(passedUrl))
			{
				commandLineHandler.HandleCommandLineArgs(new[] {
					passedUrl
				}, new AppLaunch.CommandLineEventArgs());
			}
		}
	}
}

