using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;
using LogJoint.Drawing;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class EditRegexDialogController : NSWindowController, IEditRegexDialogView
	{
		readonly IEditRegexDialogViewEvents events;
		readonly NSFont monoFont = NSFont.FromFontName("Courier", 11);
		readonly NSFont monoBoldFont = NSFont.FromFontName("Courier-Bold", 11);
		readonly CapturesDataSource capturesDataSource = new CapturesDataSource();

		public EditRegexDialogController (IEditRegexDialogViewEvents events) : base ("EditRegexDialog")
		{
			this.events = events;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				monoFont.Dispose();
				monoBoldFont.Dispose();
			}
			base.Dispose (disposing);
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			conceptsLinkLabel.StringValue = "Concepts";
			conceptsLinkLabel.LinkClicked = (sender, e) => events.OnConceptsLinkClicked();
			conceptsLinkLabel.FontSize = NSFont.SmallSystemFontSize;
			reHelpLinkLabel.StringValue = "Help on regex syntax";
			reHelpLinkLabel.LinkClicked = (sender, e) => events.OnRegexHelpLinkClicked();
			reHelpLinkLabel.FontSize = NSFont.SmallSystemFontSize;
			regexTextBox.Font = monoFont;
			regexTextBox.TextDidChange += (sender, e)  => events.OnRegExTextBoxTextChanged();
			sampleLogTextBox.Font = monoFont;
			sampleLogTextBox.TextDidChange += (sender, e) => events.OnSampleEditTextChanged();
			capturesTable.DataSource = capturesDataSource;
			capturesTable.Delegate = new CapturesDelegate() { owner = this };
		}

		partial void OnCancelClicked (Foundation.NSObject sender)
		{
			events.OnCloseButtonClicked(accepted: false);
		}

		partial void OnOkClicked (Foundation.NSObject sender)
		{
			events.OnCloseButtonClicked(accepted: true);
		}

		partial void OnTestRegexClicked (Foundation.NSObject sender)
		{
			events.OnExecRegexButtonClicked();
		}

		void IEditRegexDialogView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IEditRegexDialogView.Close ()
		{
			Close();
			NSApplication.SharedApplication.AbortModal();
		}

		string IEditRegexDialogView.ReadControl (EditRegexDialogControlId ctrl)
		{
			return (GetControl(ctrl) as NSTextView)?.Value ?? "";
		}

		void IEditRegexDialogView.WriteControl (EditRegexDialogControlId ctrl, string value)
		{
			var c = GetControl(ctrl);
			if (c is NSTextField)
				((NSTextField)c).StringValue = value;
			else if (c is NSTextView)
				((NSTextView)c).Value = value;
			else if (c is NSWindow)
				((NSWindow)c).Title = value;
		}

		void IEditRegexDialogView.ClearCapturesListBox ()
		{
			capturesDataSource.items.Clear();
			capturesTable.ReloadData();
		}

		void IEditRegexDialogView.EnableControl (EditRegexDialogControlId ctrl, bool enable)
		{
			var c = GetControl(ctrl);
			if (c is NSTextField)
				((NSTextField)c).Enabled = enable;
		}

		void IEditRegexDialogView.SetControlVisibility (EditRegexDialogControlId ctrl, bool value)
		{
			var v = GetControl(ctrl) as NSView;
			if (ctrl == EditRegexDialogControlId.EmptyReLabel)
				v = emptyReContainer;
			if (v != null)
				v.Hidden = !value;
		}

		void IEditRegexDialogView.AddCapturesListBoxItem (CapturesListBoxItem item)
		{
			capturesDataSource.items.Add(item);
			capturesTable.ReloadData();
		}

		void IEditRegexDialogView.ResetSelection (EditRegexDialogControlId ctrl)
		{
			var tv = (GetControl(ctrl) as NSTextView);
			if (tv != null)
				tv.SelectedRange = new NSRange();
		}

		void IEditRegexDialogView.PatchLogSample (TextPatch p)
		{
			var dict = new NSMutableDictionary();
			if (p.BackColor != null)
				dict[NSStringAttributeKey.BackgroundColor] = p.BackColor.Value.ToColor().ToNSColor();
			if (p.ForeColor != null)
				dict[NSStringAttributeKey.ForegroundColor] = p.ForeColor.Value.ToColor().ToNSColor();
			dict[NSStringAttributeKey.Font] = p.Bold == true ? monoBoldFont : monoFont;
			sampleLogTextBox.TextStorage.SetAttributes(dict, new NSRange(p.RangeBegin, p.RangeEnd - p.RangeBegin));
		}

		NSResponder GetControl(EditRegexDialogControlId id)
		{
			switch (id)
			{
			case EditRegexDialogControlId.Dialog: return Window;
			case EditRegexDialogControlId.RegExTextBox: return regexTextBox;
			case EditRegexDialogControlId.SampleLogTextBox: return sampleLogTextBox;
			case EditRegexDialogControlId.ReHelpLabel: return reHelpLabel;
			case EditRegexDialogControlId.EmptyReLabel: return emptyReLabel;
			case EditRegexDialogControlId.MatchesCountLabel: return matchesCountLabel;
			case EditRegexDialogControlId.PerfValueLabel: return perfRatingLabel;
			default: return null;
			}
		}

		class CapturesDataSource: NSTableViewDataSource
		{
			public List<CapturesListBoxItem> items = new List<CapturesListBoxItem>();
			public override nint GetRowCount (NSTableView tableView) => items.Count;
		};

		class CapturesDelegate: NSTableViewDelegate
		{
			public EditRegexDialogController owner;
			public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
			{
				var item = owner.capturesDataSource.items[(int)row];
				var lbl = NSLinkLabel.CreateLabel(item.Text);
				lbl.BackgroundColor = item.Color.ToColor().ToNSColor();
				return lbl;
			}
		};
	}
}
