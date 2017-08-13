using System;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.FormatIdentityPage;

namespace LogJoint.UI
{
	public partial class FormatIdentityPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		// Called when created from unmanaged code
		public FormatIdentityPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FormatIdentityPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public FormatIdentityPageController () : base ("FormatIdentityPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		public new FormatIdentityPage View {
			get {
				return (FormatIdentityPage)base.View;
			}
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetFocus (ControlId id)
		{
			View.Window.MakeFirstResponder(GetControl(id));
		}


		string IView.this [ControlId id] 
		{ 
			get => GetControl(id).StringValue; 
			set => GetControl(id).StringValue = value;
		}

		NSTextField GetControl(ControlId id)
		{
			View.EnsureCreated();
			switch (id)
			{
			case ControlId.HeaderLabel: return headerLebel;
			case ControlId.CompanyNameEdit: return companyNameTextField;
			case ControlId.FormatNameEdit: return formatNameTextField;
			case ControlId.DescriptionEdit: return descriptionTextField;
			default: return null;
			};
		}
	}
}
