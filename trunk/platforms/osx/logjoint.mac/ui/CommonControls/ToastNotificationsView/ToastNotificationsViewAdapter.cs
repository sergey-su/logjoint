
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using LogJoint.UI;

namespace LogJoint.UI
{
	public partial class ToastNotificationsViewAdapter : MonoMac.AppKit.NSViewController, IView
	{
		IViewEvents viewEvents;
		ViewItem[] lastItems;

		#region Constructors

		// Called when created from unmanaged code
		public ToastNotificationsViewAdapter (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ToastNotificationsViewAdapter (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public ToastNotificationsViewAdapter () : base ("ToastNotificationsView", NSBundle.MainBundle)
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

			// this makes views intercept mouse down and not to pass them thought to superview
			view1.OnMouseDown += (e) => {};
			view2.OnMouseDown += (e) => {};
			view3.OnMouseDown += (e) => {};
			view4.OnMouseDown += (e) => {};
		}

		//strongly typed view accessor
		public new ToastNotificationsView View {
			get {
				return (ToastNotificationsView)base.View;
			}
		}

		void IView.SetVisibility (bool visible)
		{
			View.Hidden = !visible;
		}

		void IView.Update (ViewItem[] items)
		{
			lastItems = items;
			var allCtrls = new []
			{
				new { v = view1, l = link1, p = progress1, b = suppressBtn1 },
				new { v = view2, l = link2, p = progress2, b = suppressBtn2 },
				new { v = view3, l = link3, p = progress3, b = suppressBtn3 },
				new { v = view4, l = link4, p = progress4, b = suppressBtn4 },
			};
			foreach (var i in allCtrls.ZipWithIndex())
			{
				var data = i.Key < items.Length ? items[i.Key] : null;
				var ctrls = i.Value;
				ctrls.v.Hidden = data == null;
				if (data != null)
				{
					ctrls.v.BackgroundColor = NSColor.FromCalibratedRgba(0.941f, 0.678f, 0.305f, 1f);
					ctrls.l.SetAttributedContents(data.Contents);
					ctrls.p.Hidden = data.Progress == null;
					ctrls.p.DoubleValue = data.Progress.GetValueOrDefault(0d) * 100d;
					ctrls.b.Hidden = !data.IsSuppressable;
					ctrls.l.LinkClicked = (s, e) => 
					{
						if (!string.IsNullOrEmpty(e.Link.Tag as string))
							viewEvents.OnItemActionClicked(data, (string)e.Link.Tag);
					};
					ctrls.b.Tag = i.Key;
				}
			}
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.viewEvents = eventsHandler;
		}

		partial void suppressBtnClicked (NSObject sender)
		{
			var b = sender as NSButton;
			if (b == null || lastItems == null || b.Tag >= lastItems.Length || b.Tag < 0)
				return;
			viewEvents.OnItemSuppressButtonClicked(lastItems[b.Tag]);
		}
	}
}

