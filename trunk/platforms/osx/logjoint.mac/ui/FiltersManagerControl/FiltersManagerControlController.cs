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
		Dictionary<ViewControl, NSView> ctrlsMap;

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
			bool topControlsCollapsed = 
				(controlsToShow & (ViewControl.PrevButton | ViewControl.NextButton | ViewControl.FilteringEnabledCheckbox)) == 0;
			listTopConstraint.Constant = topControlsCollapsed ? 0 : 25;
		}

		void IView.EnableControls (ViewControl controlsToEnable)
		{
			foreach (var c in ctrlsMap)
			{
				var enabled = (c.Key & controlsToEnable) != 0;
				NSControl ctrl;
				NSLinkLabel ll;
				if ((ctrl = c.Value as NSControl) != null)
					ctrl.Enabled = enabled;
				else if ((ll = c.Value as NSLinkLabel) != null)
					ll.IsEnabled = enabled;
			}
		}

		void IView.SetFiltertingEnabledCheckBoxValue (bool value, string tooltip)
		{
			enableFilteringButton.State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			enableFilteringButton.ToolTip = tooltip;
		}

		void IView.SetFiltertingEnabledCheckBoxLabel (string value)
		{
			enableFilteringButton.Title = value;
		}

		Presenters.FiltersListBox.IView IView.FiltersListView => filtersList;

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			ctrlsMap = GetCtrlMap();
			filtersList = new FiltersListController();
			filtersList.View.MoveToPlaceholder(listPlaceholder);
			link1.StringValue = "<< Prev";
			link2.StringValue = "Next >>";
			link1.LinkClicked = (sender, e) => eventsHandler.OnPrevClicked();
			link2.LinkClicked = (sender, e) => eventsHandler.OnNextClicked();
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

		partial void OnEnableFilteringClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnEnableFilteringChecked(enableFilteringButton.State == NSCellStateValue.On);
		}

		partial void OnOptionsButtonClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnOptionsClicked();
		}

		partial void OnMoveUpClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnMoveFilterUpClicked();
		}

		partial void OnMoveDownClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnMoveFilterDownClicked();
		}

		Dictionary<ViewControl, NSView> GetCtrlMap()
		{
			return new Dictionary<ViewControl, NSView>()
			{
				{ ViewControl.AddFilterButton, addFilterButton },
				{ ViewControl.RemoveFilterButton, removeFilterButton },
				{ ViewControl.FilteringEnabledCheckbox, enableFilteringButton },
				{ ViewControl.PrevButton, link1 },
				{ ViewControl.NextButton, link2 },
				{ ViewControl.FilterOptions, optionsButton },
				{ ViewControl.MoveUpButton, moveUpButton },
				{ ViewControl.MoveDownButton, moveDownButton },
			};
		}
	}
}
