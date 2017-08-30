using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard;

namespace LogJoint.UI
{
	public partial class FormatsWizardDialogController : NSWindowController, IView
	{
		IViewEvents eventsHandler;

		public FormatsWizardDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public FormatsWizardDialogController (NSCoder coder) : base (coder)
		{
		}

		public FormatsWizardDialogController () : base ("FormatsWizardDialog")
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		public new FormatsWizardDialog Window => (FormatsWizardDialog)base.Window;

		partial void backClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnBackClicked();
		}

		partial void cancelClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnCloseClicked();
		}

		partial void nextClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnNextClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.ShowDialog ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.CloseDialog ()
		{
			NSApplication.SharedApplication.StopModal ();
			this.Close();
		}

		void IView.SetControls (string backText, bool backEnabled, string nextText, bool nextEnabled)
		{
			Window.GetType();
			backButton.Title = backText;
			backButton.Enabled = backEnabled;
			nextButton.Title = nextText;
			nextButton.Enabled = nextEnabled;
		}

		void IView.HidePage (object viewObject)
		{
			var view = (viewObject as NSViewController)?.View;
			if (view != null)
				view.Hidden = true;
		}

		void IView.ShowPage (object viewObject)
		{
			var view = (viewObject as NSViewController)?.View;
			if (view == null)
				return;
			Window.GetType();
			view.MoveToPlaceholder(pagePlaceholder);
			view.Hidden = false;
		}
	}
}
