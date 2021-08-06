
using System;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.TimelinePanel;

namespace LogJoint.UI
{
	public partial class TimelinePanelControlAdapter : NSViewController, IView
	{
		IViewModel viewModel;
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

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			viewModel.ChangeNotification.CreateSubscription (
				Updaters.Create (() => viewModel.IsEnabled, enabled => {
					foreach (var c in new [] { zoomInButton, zoomOutButton,
							resetZoomButton, moveUpButton, moveDownButton }) {
						c.Enabled = enabled;
					}
				}
			));
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
			viewModel.OnScrollToolButtonClicked(1);
		}

		partial void OnMoveUpClicked (Foundation.NSObject sender)
		{
			viewModel.OnScrollToolButtonClicked(-1);
		}

		partial void OnResetZoomClicked (Foundation.NSObject sender)
		{
			viewModel.OnZoomToViewAllToolButtonClicked();
		}

		partial void OnZoomInClicked (Foundation.NSObject sender)
		{
			viewModel.OnZoomToolButtonClicked(1);
		}

		partial void OnZoomOutClicked (Foundation.NSObject sender)
		{
			viewModel.OnZoomToolButtonClicked(-1);
		}
	}
}

