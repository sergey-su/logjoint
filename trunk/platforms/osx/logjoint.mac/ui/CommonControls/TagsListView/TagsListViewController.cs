using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI;
using LogJoint.UI.Presenters.TagsList;

namespace LogJoint.UI
{
	public partial class TagsListViewController : MonoMac.AppKit.NSViewController, IView
	{			
		IViewEvents eventsHandler;

		#region Constructors

		// Called when created from unmanaged code
		public TagsListViewController (IntPtr handle) : base (handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TagsListViewController (NSCoder coder) : base (coder)
		{
		}
		
		// Call to load from the XIB/NIB file
		public TagsListViewController () : base ("TagsListView", NSBundle.MainBundle)
		{
		}
		
		#endregion


		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetText (string value, int clickablePartBegin, int clickablePartLength)
		{
			linkLabel.StringValue = value;
			linkLabel.Links = new [] { new NSLinkLabel.Link(clickablePartBegin, clickablePartLength) };
		}

		HashSet<string> IView.RunEditDialog (Dictionary<string, bool> tags)
		{
			return TagsSelectionSheetController.Run(tags, View.Window);
		}

		void IView.SetSingleLine (bool value)
		{
			linkLabel.SingleLine = value;
		}
			
		new TagsListView View 
		{
			get { return (TagsListView)base.View; }
		}
			
		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			linkLabel.SingleLine = true;
			linkLabel.LinkClicked = (s, e) => eventsHandler.OnEditLinkClicked();
		}
	}
}

