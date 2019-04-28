
using System;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelinePanelControlAdapter : NSViewController, IView
	{
		IViewEvents viewEvents;
		TimelineControlAdapter timelineControlAdapter;

		#region Constructors

		// Called when created from unmanaged code
		public TimelinePanelControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public TimelinePanelControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public TimelinePanelControlAdapter()
			: base("TimelinePanelControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
			timelineControlAdapter = new TimelineControlAdapter();
		}

		#endregion

		public new TimelinePanelControl View
		{
			get
			{
				return (TimelinePanelControl)base.View;
			}
		}

		public TimelineControlAdapter TimelineControlAdapter
		{
			get { return timelineControlAdapter; }
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetEnabled(bool value)
		{
			foreach (var c in new[] { zoomInButton, zoomOutButton, 
				resetZoomButton, moveUpButton, moveDownButton })
			{
				c.Enabled = value;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			timelineControlAdapter.View.MoveToPlaceholder(timelineControlPlaceholder);
			zoomInButton.Image.Template = true;
			zoomOutButton.Image.Template = true;
			resetZoomButton.Image.Template = true;
			moveDownButton.Image.Template = true;
			moveUpButton.Image.Template = true;
		}

		partial void OnMoveDownClicked (Foundation.NSObject sender)
		{
			viewEvents.OnScrollToolButtonClicked(1);
		}

		partial void OnMoveUpClicked (Foundation.NSObject sender)
		{
			viewEvents.OnScrollToolButtonClicked(-1);
		}

		partial void OnResetZoomClicked (Foundation.NSObject sender)
		{
			viewEvents.OnZoomToViewAllToolButtonClicked();
		}

		partial void OnZoomInClicked (Foundation.NSObject sender)
		{
			viewEvents.OnZoomToolButtonClicked(1);
		}

		partial void OnZoomOutClicked (Foundation.NSObject sender)
		{
			viewEvents.OnZoomToolButtonClicked(-1);
		}
	}
}

