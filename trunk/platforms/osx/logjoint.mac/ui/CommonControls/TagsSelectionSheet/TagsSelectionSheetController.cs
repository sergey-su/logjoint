using System;
using System.Linq;
using Foundation;
using AppKit;
using System.Collections.Generic;
using LogJoint.UI.Presenters.TagsList;

namespace LogJoint.UI
{
	public partial class TagsSelectionSheetController: NSWindowController, IDialogView
	{
		readonly NSWindow parentWindow;
		readonly IDialogViewModel viewModel;
		List<NSButton> views = new List<NSButton>();
		ISubscription subscription;

		public TagsSelectionSheetController (
			NSWindow parentWindow, IDialogViewModel viewModel)
		{
			this.parentWindow = parentWindow;
			this.viewModel = viewModel;
			NSBundle.LoadNib ("TagsSelectionSheet", this);
		}

		public static IDialogView CreateDialog (
			NSWindow parentWindow,
			IChangeNotification changeNotification,
			IDialogViewModel viewModel,
			IEnumerable<string> tags,
			string initiallyFocusedTag
		)
		{
			var dlg = new TagsSelectionSheetController (parentWindow, viewModel);
			dlg.Window.GetHashCode ();

			int focusedRow = -1;
			foreach (var t in tags) {
				var b = new NSButton () {
					Title = t,
					State = NSCellStateValue.Off
				};
				b.SetButtonType (NSButtonType.Switch);
				if (initiallyFocusedTag == t)
					focusedRow = dlg.views.Count;
				b.Action = new ObjCRuntime.Selector ("OnTagChecked:");
				b.Target = dlg;
				dlg.views.Add (b);
			}
			dlg.table.Delegate = new Delegate () { owner = dlg };
			dlg.table.DataSource = new DataSource () { owner = dlg };
			if (focusedRow >= 0) {
				dlg.table.SelectRow (focusedRow, byExtendingSelection: false);
				dlg.table.ScrollRowToVisible (focusedRow);
			}
			dlg.linkLabel.StringValue = "select: all   none";
			dlg.linkLabel.Links = new [] {
				new NSLinkLabel.Link(8, 3, ""),
				new NSLinkLabel.Link(14, 4, null),
			};
			dlg.formulaTextView.Delegate = new TextViewDelegate()
			{
				view = dlg.formulaTextView,
				viewModel = viewModel,
				changeNotification = changeNotification
			};

			dlg.linkLabel.LinkClicked = (s, e) => {
				if (e.Link.Tag != null) viewModel.OnUseAllClicked ();
				else viewModel.OnUnuseAllClicked ();
			};

			var updateCheckboxes = Updaters.Create (
				() => viewModel.SelectedTags,
				() => viewModel.IsEditingFormula,
				(selected, editing) => {
					dlg.views.ForEach (b => {
						b.State = selected.Contains(b.Title) ? NSCellStateValue.On : NSCellStateValue.Off;
						b.Enabled = !editing;
					});
				}
			);

			NSColor getLinkColor(MessageSeverity sev) => 
				sev == MessageSeverity.Error ? NSColor.Red :
				sev == MessageSeverity.Warning ? NSColor.Orange :
				NSColor.Text;

			var updateFormula = Updaters.Create (
				() => viewModel.Formula,
				() => viewModel.IsEditingFormula,
				() => viewModel.FormulaStatus,
				(formula, editing, status) => {
					if (dlg.formulaTextView.Value != formula)
						dlg.formulaTextView.Value = formula;
					dlg.formulaTextView.Editable = editing;
					dlg.formulaTextView.BackgroundColor = editing ? NSColor.TextBackground : NSColor.ControlBackground;
					dlg.formulaTextView.TextColor = editing ? NSColor.Text : NSColor.PlaceholderTextColor;
					dlg.linkLabel.IsEnabled = !editing;
					dlg.okButton.Enabled = !editing;
					dlg.formulaEditLinkLabel.StringValue = editing ? "done" : "edit";
					var (statusText, statusSeverity) = status;
					dlg.formulaLinkLabel.SetAttributedContents(statusText);
					dlg.formulaEditLinkLabel.IsEnabled = statusSeverity != MessageSeverity.Error;
					dlg.formulaLinkLabel.TextColor = getLinkColor(statusSeverity);
				}
			);

			var formulaFocusSideEffect = Updaters.Create(
				() => viewModel.IsEditingFormula,
				editing =>
				{
					if (editing)
						dlg.Window.MakeFirstResponder(dlg.formulaTextView);
				}
			);

			var updateSuggestions = Updaters.Create(
				() => viewModel.FormulaSuggesions,
				value =>
				{
					var (list, selectedItem) = value;
					dlg.suggestionsContainer.Hidden = list.IsEmpty;
					dlg.suggestionsLabel.Hidden = list.IsEmpty;
					dlg.suggestionsView.Subviews.ToList().ForEach(v => v.RemoveFromSuperview());
					var itemHeight = 15;
					nfloat maxRight = 0;
					var views = list.Select((str, idx) =>
					{
						var lbl = NSLinkLabel.CreateLabel(str);
						lbl.BackgroundColor = idx == selectedItem ? NSColor.SelectedTextBackground : NSColor.Clear;
						lbl.TextColor = idx == selectedItem ? NSColor.SelectedText : NSColor.Text;
						lbl.LinkClicked = (s, e) => viewModel.OnSuggestionClicked(idx);
						lbl.SetFrameOrigin(new CoreGraphics.CGPoint(5, idx * itemHeight));
						lbl.SetFrameSize(lbl.IntrinsicContentSize);
						return lbl;
					}).ToList();
					foreach (var subView in views) {
						dlg.suggestionsView.AddSubview(subView);
						maxRight = (nfloat)Math.Max(maxRight, subView.Frame.Right);
					}
					dlg.suggestionsView.SetFrameSize(new CoreGraphics.CGSize(maxRight, list.Length * itemHeight));
					if (selectedItem != null)
						dlg.suggestionsView.ScrollRectToVisible(views[selectedItem.Value].Frame);
				}
			);

			var listStatusUpdater = Updaters.Create(
				() => viewModel.TagsListStatus,
				(status) =>
				{
					var (statusText, statusSeverity) = status;
					dlg.tagsStatusLinkLabel.SetAttributedContents(statusText);
					dlg.tagsStatusLinkLabel.TextColor = getLinkColor(statusSeverity);
				}
			);

			dlg.subscription = changeNotification.CreateSubscription (() => {
				updateCheckboxes ();
				updateFormula ();
				formulaFocusSideEffect ();
				updateSuggestions ();
				listStatusUpdater ();
			}, initiallyActive: false);

			dlg.formulaEditLinkLabel.LinkClicked = (sender, e) => {
				if (viewModel.IsEditingFormula)
					viewModel.OnStopEditingFormulaClicked();
				else
					viewModel.OnEditFormulaClicked();
			};

			dlg.formulaLinkLabel.LinkClicked = (sender, e) =>
				viewModel.OnFormulaLinkClicked(e.Link.Tag as string);

			dlg.tagsStatusLinkLabel.LinkClicked = (sender, e) =>
				viewModel.OnTagsStatusLinkClicked(e.Link.Tag as string);


			return dlg;
		}

		partial void OnCancelled (NSObject sender)
		{
			viewModel.OnCancelDialog ();
		}

		partial void OnConfirmed (NSObject sender)
		{
			viewModel.OnConfirmDialog ();
		}

		void IDialogView.Open ()
		{
			subscription.Active = true;
			NSApplication.SharedApplication.BeginSheet (Window, parentWindow);
			NSApplication.SharedApplication.RunModalForWindow (Window);
		}

		void IDialogView.Close ()
		{
			subscription.Active = false;
			NSApplication.SharedApplication.EndSheet (Window);
			Window.Close ();
			NSApplication.SharedApplication.StopModal ();
		}

		int IDialogView.FormulaCursorPosition
		{
			get => (int)formulaTextView.SelectedRange.Location;
			set {
				subscription.SideEffect();
				formulaTextView.SelectedRange = new NSRange(value, 0);
				Window.MakeFirstResponder(formulaTextView);
			}
		}

		void IDialogView.OpenFormulaTab()
		{
			tabView.SelectAt(1);
		}

		[Export("OnTagChecked:")]
		void OnChecked(NSButton sender)
		{
			if (sender is NSButton btn) {
				if (btn.State == NSCellStateValue.Off)
					viewModel.OnUnuseTagClicked(btn.Title);
				else
					viewModel.OnUseTagClicked(btn.Title);
			}
		}


		class DataSource : NSTableViewDataSource
		{
			public TagsSelectionSheetController owner;

			public override nint GetRowCount (NSTableView tableView) { return owner.views.Count; }
		};

		class Delegate: NSTableViewDelegate
		{
			public TagsSelectionSheetController owner;

			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				return owner.views [(int)row];
			}
		};

		class TextViewDelegate: NSTextViewDelegate
		{
			public IDialogViewModel viewModel;
			public IChangeNotification changeNotification;
			public NSTextView view;

			public override bool DoCommandBySelector (NSTextView textView, ObjCRuntime.Selector commandSelector)
			{
				var k = KeyCode.None;
			    if (commandSelector.Name == "moveUp:"){
			        k = KeyCode.Up;
			    }
				else if (commandSelector.Name == "moveDown:"){
			        k = KeyCode.Down;
			    }
				else if (commandSelector.Name == "insertNewline:"){
					k = KeyCode.Enter;
			    }
				return viewModel.OnFormulaKeyPressed(k);
			}

			public override void DidChangeSelection (NSNotification notification)
			{
				changeNotification.Post();
			}

			public override void TextDidChange (NSNotification notification)
			{
				viewModel.OnFormulaChange(view.Value);
			}
		};
	}
}

