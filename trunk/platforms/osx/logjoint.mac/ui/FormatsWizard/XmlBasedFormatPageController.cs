using System;

using Foundation;
using AppKit;

using LogJoint.Drawing;
using LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage;

namespace LogJoint.UI
{
	public partial class XmlBasedFormatPageController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public XmlBasedFormatPageController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public XmlBasedFormatPageController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public XmlBasedFormatPageController () : base ("XmlBasedFormatPage", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			helpLinkLabel.StringValue = "Concepts";
			helpLinkLabel.LinkClicked = (sender, e) => eventsHandler.OnConceptsLinkClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetLabelProps (ControlId labelId, string text, ModelColor? color)
		{
			var ctrl = GetControl(labelId);
			if (ctrl == null)
				return;
			ctrl.StringValue = text;
			if (color != null)
				ctrl.TextColor = color.Value.ToColor().ToNSColor();
		}

		partial void OnEditHeaderReClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnChangeHeaderReButtonClicked();
		}

		partial void OnEditXsltClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnChangeTransformButtonClicked();
		}

		partial void OnSelectSampleLogClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnSelectSampleButtonClicked();
		}

		partial void OnTestClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnTestButtonClicked();
		}


		NSTextField GetControl(ControlId id)
		{
			View.EnsureCreated();
			switch (id)
			{
			case ControlId.PageTitleLabel: return pageTitleLabel;
			case ControlId.ConceptsLabel: return conceptsLabel;
			case ControlId.SampleLogLabel: return sampleLogLabel;
			case ControlId.SampleLogStatusLabel: return sampleLogStatusLabel;
			case ControlId.HeaderReLabel: return headerReLabel;
			case ControlId.HeaderReStatusLabel: return headerReStatusLabel;
			case ControlId.TransformLabel: return transformLabel;
			case ControlId.TransformStatusLabel: return xsltStatusLabel;
			case ControlId.TestLabel: return testLabel;
			case ControlId.TestStatusLabel: return testStatusLabel;
			default: return null;
			}
		}

	}
}
