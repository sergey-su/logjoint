using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.SearchesManagerDialog;

namespace LogJoint.UI
{
	public partial class SearchesManagerDialogController : NSWindowController, IDialogView
	{
		IDialogViewEvents eventsHandler;

		public SearchesManagerDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchesManagerDialogController (NSCoder coder) : base (coder)
		{
		}

		public SearchesManagerDialogController (IDialogViewEvents eventsHandler) : base ("SearchesManagerDialog")
		{
			this.eventsHandler = eventsHandler;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		void IDialogView.SetItems (ViewItem [] items)
		{
			// todo
		}

		ViewItem [] IDialogView.GetSelectedItems ()
		{
			// todo
			return new ViewItem[0];
		}

		void IDialogView.EnableControl (ViewControl id, bool value)
		{
			// todo
		}

		void IDialogView.OpenModal ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IDialogView.CloseModal ()
		{
			NSApplication.SharedApplication.StopModal();
		}

		public new SearchesManagerDialog Window 
		{
			get { return (SearchesManagerDialog)base.Window; }
		}
	}

	public class SearchesManagerDialogView: IView
	{
		IDialogView IView.CreateDialog (IDialogViewEvents eventsHandler)
		{
			return new SearchesManagerDialogController(eventsHandler);
		}
	};
}
