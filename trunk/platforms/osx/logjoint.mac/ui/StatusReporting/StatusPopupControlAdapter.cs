
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.StatusReports;
using System.Text;

namespace LogJoint.UI
{
	public partial class StatusPopupControlAdapter : 
		NSViewController,
		IView
	{
		IViewEvents viewEvents;

		#region Constructors

		// Called when created from unmanaged code
		public StatusPopupControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public StatusPopupControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public StatusPopupControlAdapter()
			: base("StatusPopupControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new StatusPopupControl View
		{
			get
			{
				return (StatusPopupControl)base.View;
			}
		}

		void IView.SetViewEvents(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetStatusText(string value)
		{
			// todo
		}

		void IView.HidePopup()
		{
			SetVisibility(false);
		}

		void IView.ShowPopup(string caption, IEnumerable<MessagePart> parts)
		{
			captionLabel.StringValue = caption;
			var contentText = new StringBuilder();
			var links = new List<NSLinkLabel.Link>();
			foreach (var part in parts)
			{
				contentText.Append(part.Text);
				var link = part as MessageLink;
				if (link != null)
				{
					links.Add(new NSLinkLabel.Link(contentText.Length - part.Text.Length, 
						part.Text.Length, link.Click));
				}
			}
			if (links.Count == 0)
			{
				links.Add(new NSLinkLabel.Link(0, 0, null));
			}
			contentLinkLabel.StringValue = contentText.ToString();
			contentLinkLabel.Links = links;
			SetVisibility(true);
		}

		void IView.SetCancelLongRunningControlsVisibility(bool value)
		{
			// todo
		}

		void SetVisibility(bool visible)
		{
			View.Superview.Hidden = !visible;
			/* 
			 * animation below does not work :( 
			 * todo: make it work
			 * maybe animated view should be layered.
			 *
			var animationDict = new NSMutableDictionary();
			animationDict[NSViewAnimation.TargetKey] = contentLinkLabel;
			animationDict[NSViewAnimation.EffectKey] = 
				visible ? NSViewAnimation.FadeInEffect : NSViewAnimation.FadeOutEffect;
			var anim = new NSViewAnimation(new [] {animationDict})
			{
				Duration = 1.0,
				AnimationCurve = NSAnimationCurve.EaseIn,
				AnimationBlockingMode = NSAnimationBlockingMode.Nonblocking
			};
			anim.StartAnimation();*/
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			contentLinkLabel.LinkClicked = (s, e) =>
			{
				var handler = e.Link.Tag as Action;
				if (handler != null)
					handler();
			};
		}
	}
}

