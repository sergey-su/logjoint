using System;
using Foundation;
using AppKit;

using LogJoint.Drawing;
using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class RegexBasedFormatPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		// Called when created from unmanaged code
		public RegexBasedFormatPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public RegexBasedFormatPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public RegexBasedFormatPageController () : base ("RegexBasedFormatPage", NSBundle.MainBundle)
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
			helpLinkLabel.StringValue = "Concepts";
			helpLinkLabel.LinkClicked = (sender, e) => eventsHandler.OnConceptsLinkClicked();
		}

		partial void OnEditBodyReClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnChangeBodyReButtonClicked();
		}

		partial void OnEditFieldsMappingClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnChangeFieldsMappingButtonClick();
		}

		partial void OnEditHeaderReClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnChangeHeaderReButtonClicked();
		}

		partial void OnSelectSampleLogClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnSelectSampleButtonClicked();
		}

		partial void OnTestClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnTestButtonClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			View.EnsureCreated();
		}

		void IView.SetLabelProps (ControlId labelId, string text, ModelColor color)
		{
			var ctrl = GetControl(labelId);
			if (ctrl == null)
				return;
			ctrl.StringValue = text;
			ctrl.TextColor = color.ToColor().ToNSColor();
		}

		IFieldsMappingDialogView IView.CreateFieldsMappingDialogView (IFieldsMappingDialogViewEvents eventsHandler)
		{
			return new FieldsMappingDialogController(eventsHandler);
		}

		NSTextField GetControl(ControlId id)
		{
			switch (id)
			{
				case ControlId.HeaderReStatusLabel: return headerReStatusLabel;
				case ControlId.BodyReStatusLabel: return bodyReStatusLabel;
				case ControlId.FieldsMappingLabel: return fieldsMappingLabel;
				case ControlId.TestStatusLabel: return testStatusLabel;
				case ControlId.SampleLogStatusLabel: return sampleLogStatusLabel;
				default: return null;
			}
		}
	}
}
