
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
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

		void IView.SetViewTailModeToolButtonState(bool buttonChecked)
		{
			// todo
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			timelineControlAdapter.View.MoveToPlaceholder(timelineControlPlaceholder);
		}
	}
}

