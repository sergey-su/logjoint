
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using ObjCRuntime;
using LogJoint.UI.Presenters.PreprocessingUserInteractions;
using LogJoint.UI.Reactive;
using static LogJoint.UI.UIUtils;

namespace LogJoint.UI
{
	public partial class FilesSelectionDialogController : AppKit.NSWindowController
	{
		readonly IViewModel viewModel;
		readonly Mac.IReactive reactive;
		INSTableViewController<IDialogItem> tableController;

		#region Constructors

		// Call to load from the XIB/NIB file
		public FilesSelectionDialogController (IViewModel viewModel, Mac.IReactive reactive)
			: base("FilesSelectionDialog")
		{
			this.viewModel = viewModel;
			this.reactive = reactive;
		}
		
		#endregion

		public new FilesSelectionDialog Window
		{
			get
			{
				return (FilesSelectionDialog)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.owner = this;

			this.tableController = reactive.CreateTableViewController<IDialogItem> (tableView);
			this.tableController.OnCreateView = (item, column) => {
				var view = new NSButton {
					State = NSCellStateValue.Off,
					Action = ActionTarget.ActionSelector
				};
				view.SetButtonType (NSButtonType.Switch);
				UpdateButton (item, view);
				return view;
			};
			this.tableController.OnUpdateView = (item, column, view, old) => UpdateButton (item, (NSButton)view);
			this.tableController.OnSelect = items => viewModel.OnSelect (items.LastOrDefault ());

			checkAllButton.StringValue = "check all";
			checkAllButton.LinkClicked = (s, e) => viewModel.OnCheckAll (true);
			uncheckAllButton.StringValue = "uncheck all";
			uncheckAllButton.LinkClicked = (s, e) => viewModel.OnCheckAll (false);
		}

		void UpdateButton (IDialogItem item, NSButton btn)
		{
			btn.Title = item.Title;
 			btn.Target = new ActionTarget (_ => viewModel.OnCheck(item, btn.State == NSCellStateValue.On));
			btn.State = item.IsChecked ? NSCellStateValue.On : NSCellStateValue.Off;
		}

		public void Update (DialogViewData dd)
		{
			Window.Title = dd.Title;
			tableController.Update (dd.Items);
		}

		public static void Execute(IViewModel viewModel, Mac.IReactive reactive, out FilesSelectionDialogController dialog)
		{
			dialog = new FilesSelectionDialogController (viewModel, reactive);
			dialog.Update (viewModel.DialogData);
			NSApplication.SharedApplication.RunModalForWindow (dialog.Window);
		}

		public new void Close ()
		{
			NSApplication.SharedApplication.StopModal ();
			base.Close ();
		}

		partial void OnCancelButtonClicked (NSObject sender) => viewModel.OnCloseDialog (accept: false);

		partial void OnOpenButtonClicked (NSObject sender) => viewModel.OnCloseDialog (accept: true);
	}
}

