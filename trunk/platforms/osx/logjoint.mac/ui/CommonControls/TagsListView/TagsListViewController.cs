using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI;
using LogJoint.UI.Presenters.TagsList;

namespace LogJoint.UI
{
	public partial class TagsListViewController : AppKit.NSViewController, IView
	{
		IViewModel eventsHandler;

		public TagsListViewController () : 
			base ("TagsListView", NSBundle.MainBundle)
		{
		}

		void IView.SetViewModel (IViewModel eventsHandler)
		{
			this.View.EnsureCreated();
			this.eventsHandler = eventsHandler;

			var linkTextUpdater = Updaters.Create (
				() => eventsHandler.EditLinkValue,
				(value) => {
					var (str, clickablePartBegin, clickablePartLength) = value;
					linkLabel.StringValue = str;
					linkLabel.Links = new [] { new NSLinkLabel.Link (clickablePartBegin, clickablePartLength) };
				}
			);
			var linkIsSingleLineUpdater = Updaters.Create (
				() => eventsHandler.IsSingleLine,
				(value) => linkLabel.SingleLine = value
			);
			eventsHandler.ChangeNotification.CreateSubscription (() => {
				linkTextUpdater ();
				linkIsSingleLineUpdater ();
			});
		}

		IDialogView IView.CreateDialog (
			IDialogViewModel dialogViewModel,
			IEnumerable<string> tags,
			string initiallyFocusedTag
		)
		{
			return TagsSelectionSheetController.CreateDialog (
				View.Window, this.eventsHandler.ChangeNotification, dialogViewModel, tags, initiallyFocusedTag);
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

