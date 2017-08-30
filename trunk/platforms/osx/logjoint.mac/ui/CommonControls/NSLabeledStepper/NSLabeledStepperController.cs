using System;
using Foundation;
using AppKit;

using LogJoint.UI.Presenters.LabeledStepperPresenter;

namespace LogJoint.UI
{
	public partial class NSLabeledStepperController : AppKit.NSViewController, IView
	{
		IViewEvents events;
		int prevValue;

		// Called when created from unmanaged code
		public NSLabeledStepperController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NSLabeledStepperController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public NSLabeledStepperController () : base ("NSLabeledStepper", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			prevValue = stepper.IntValue;
		}

		void IView.SetEventsHandler (IViewEvents handler)
		{
			this.events = handler;
		}

		void IView.SetLabel (string value)
		{
			label.StringValue = value;
		}

		void IView.EnableControls (bool enableUp, bool enableDown, bool enableLabel)
		{
			stepper.Enabled = enableUp | enableDown;
			label.TextColor = enableLabel ? NSColor.Text : NSColor.DisabledControlText;
		}

		partial void stepperAction (Foundation.NSObject sender)
		{
			var val = stepper.IntValue;
			if (val > prevValue) // yeah, overflows are not handled
				events.OnUpButtonClicked();
			else if (val < prevValue)
				events.OnDownButtonClicked();
			prevValue = val;
		}
	}
}
