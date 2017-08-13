using System;
using System.Linq;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.ChooseOperationPage;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class ChooseOperationPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		Dictionary<ControlId, NSButton> controls;

		#region Constructors

		// Called when created from unmanaged code
		public ChooseOperationPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ChooseOperationPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public ChooseOperationPageController () : base ("ChooseOperationPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		#endregion

		//strongly typed view accessor
		public new ChooseOperationPage View {
			get {
				return (ChooseOperationPage)base.View;
			}
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			controls = new Dictionary<ControlId, NSButton>()
			{
				{ ControlId.ChangeFormatButton, changeFormatButton },
				{ ControlId.ImportNLogButton, importNLogButton },
				{ ControlId.ImportLog4NetButton, importLog4NetButton },
				{ ControlId.NewREBasedButton, newREBasedFormatButton },
			};
			foreach (var c in controls.Values.OfType<NSExtendedButton>())
			{
				c.OnDblClicked += (sender, e) => eventsHandler.OnOptionDblClicked();
			}
		}

		partial void OnRadioButtonSelected (Foundation.NSObject sender)
		{
		}

		ControlId IView.SelectedControl
		{
			get
			{
				View.EnsureCreated();
				foreach (var ctrl in controls)
					if (ctrl.Value.State == NSCellStateValue.On)
						return ctrl.Key;
				return ControlId.None;
			}
		}
	}
}
