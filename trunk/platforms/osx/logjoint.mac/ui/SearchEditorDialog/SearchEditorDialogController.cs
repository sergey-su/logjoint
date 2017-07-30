using System;

using Foundation;
using AppKit;
using LogJoint.UI.Presenters.SearchEditorDialog;

namespace LogJoint.UI
{
	public partial class SearchEditorDialogController : NSWindowController, IDialogView
	{
		FiltersManagerControlController filtersManagerControlController;
		IDialogViewEvents eventsHandler;

		public SearchEditorDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchEditorDialogController (NSCoder coder) : base (coder)
		{
		}

		public SearchEditorDialogController (IDialogViewEvents eventsHandler) : base ("SearchEditorDialog")
		{
			this.eventsHandler = eventsHandler;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			filtersManagerControlController = new FiltersManagerControlController();
			filtersManagerControlController.View.MoveToPlaceholder(filtersManagerViewPlaceholder);
		}

		void IDialogView.OpenModal ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IDialogView.CloseModal ()
		{
			NSApplication.SharedApplication.StopModal();
			this.Close();
		}

		Presenters.FiltersManager.IView IDialogView.FiltersManagerView
		{
			get 
			{
				Window.GetHashCode(); 
				return filtersManagerControlController; 
			}
		}

		void IDialogView.SetData(DialogData data)
		{
			nameTextBox.StringValue = data.Name;
		}

		DialogData IDialogView.GetData()
		{
			return new DialogData()
			{
				Name = nameTextBox.StringValue
			};
		}

		public new SearchEditorDialog Window 
		{
			get { return (SearchEditorDialog)base.Window; }
		}

		partial void OnCancelled (NSObject sender)
		{
			eventsHandler.OnCancelled();
		}

		partial void OnConfirmed (NSObject sender)
		{
			eventsHandler.OnConfirmed();
		}
	}

	public class SearchEditorDialogView : IView
	{
		IDialogView IView.CreateDialog (IDialogViewEvents eventsHandler)
		{
			return new SearchEditorDialogController(eventsHandler);
		}
	};
}
