using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.FiltersManager;

namespace LogJoint.UI
{
	public partial class FiltersManagerControlController : AppKit.NSViewController, IView
	{
		IViewEvents eventsHandler;
		FiltersListController filtersList;
		Dictionary<ViewControl, NSControl> ctrlsMap;

		// Called when created from unmanaged code
		public FiltersManagerControlController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public FiltersManagerControlController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Call to load from the XIB/NIB file
		public FiltersManagerControlController () : base ("FiltersManagerControl", NSBundle.MainBundle)
		{
			Initialize ();
		}

		// Shared initialization code
		void Initialize ()
		{
		}

		void IView.SetPresenter (IViewEvents presenter)
		{
			this.eventsHandler = presenter;
		}

		void IView.SetControlsVisibility (ViewControl controlsToShow)
		{
			foreach (var c in ctrlsMap)
				c.Value.Hidden = (c.Key & controlsToShow) == 0;
		}

		void IView.EnableControls (ViewControl controlsToEnable)
		{
			foreach (var c in ctrlsMap)
				c.Value.Enabled = (c.Key & controlsToEnable) != 0;
		}

		void IView.SetFiltertingEnabledCheckBoxValue (bool value)
		{
			// todo
		}

		void IView.SetFiltertingEnabledCheckBoxLabel (string value)
		{
			// todo
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			ctrlsMap = GetCtrlMap();
			filtersList = new FiltersListController();
			filtersList.View.MoveToPlaceholder(listPlaceholder);
		}

		public new FiltersManagerControl View => (FiltersManagerControl)base.View;

		public FiltersListController FiltersList => filtersList;

		partial void OnAddFilterClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnAddFilterClicked();
		}

		partial void OnDeleteFilterClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnRemoveFilterClicked();
		}

		partial void OnMoveDownClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnMoveFilterDownClicked();
		}

		partial void OnMoveUpClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnMoveFilterUpClicked();
		}

		Dictionary<ViewControl, NSControl> GetCtrlMap()
		{
			// todo: impl other controls
			return new Dictionary<ViewControl, NSControl>()
			{
				{ ViewControl.AddFilterButton, addFilterButton },
				{ ViewControl.RemoveFilterButton, removeFilterButton },
				{ ViewControl.MoveUpButton, moveUpButton },
				{ ViewControl.MoveDownButton, moveDownButton },
			};
		}
	}
}
