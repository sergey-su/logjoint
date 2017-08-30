using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.FormatsWizard.EditRegexDialog;
using LogJoint.Drawing;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class EditRegexDialogController : NSWindowController, IView
	{
		readonly NSFont monoFont = NSFont.FromFontName("Courier", 11);
		readonly NSFont monoBoldFont = NSFont.FromFontName("Courier-Bold", 11);
		readonly CapturesDataSource capturesDataSource = new CapturesDataSource();
		IViewEvents events;

		public EditRegexDialogController () : base ("EditRegexDialog")
		{
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

		void IView.SetEventsHandler (IViewEvents events)
		{
			this.events = events;
		}

		void IView.Show ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IView.Close ()
		{
			Close();
			NSApplication.SharedApplication.AbortModal();
		}

		string IView.ReadControl (ControlId ctrl)
		{
			return (GetControl(ctrl) as NSTextView)?.Value ?? "";
		}

		void IView.WriteControl (ControlId ctrl, string value)
		{
			var c = GetControl(ctrl);
			if (c is NSTextField)
				((NSTextField)c).StringValue = value;
			else if (c is NSTextView)
				((NSTextView)c).Value = value;
			else if (c is NSWindow)
				((NSWindow)c).Title = value;
		}

		void IView.ClearCapturesListBox ()
		{
			capturesDataSource.items.Clear();
			capturesTable.ReloadData();
		}

		void IView.EnableControl (ControlId ctrl, bool enable)
		{
			var c = GetControl(ctrl);
			if (c is NSTextField)
				((NSTextField)c).Enabled = enable;
		}

		void IView.SetControlVisibility (ControlId ctrl, bool value)
		{
			var v = GetControl(ctrl) as NSView;
			if (ctrl == ControlId.EmptyReLabel)
				v = emptyReContainer;
			if (v != null)
				v.Hidden = !value;
		}

		void IView.AddCapturesListBoxItem (CapturesListBoxItem item)
		{
			capturesDataSource.items.Add(item);
			capturesTable.ReloadData();
		}

		void IView.ResetSelection (ControlId ctrl)
		{
			var tv = (GetControl(ctrl) as NSTextView);
			if (tv != null)
				tv.SelectedRange = new NSRange();
		}

		void IView.PatchLogSample (TextPatch p)
		{
			var dict = new NSMutableDictionary();
			if (p.BackColor != null)
				dict[NSStringAttributeKey.BackgroundColor] = p.BackColor.Value.ToColor().ToNSColor();
			if (p.ForeColor != null)
				dict[NSStringAttributeKey.ForegroundColor] = p.ForeColor.Value.ToColor().ToNSColor();
			dict[NSStringAttributeKey.Font] = p.Bold == true ? monoBoldFont : monoFont;
			sampleLogTextBox.TextStorage.SetAttributes(dict, new NSRange(p.RangeBegin, p.RangeEnd - p.RangeBegin));
		}

		NSResponder GetControl(ControlId id)
		{
			switch (id)
			{
			case ControlId.Dialog: return Window;
			case ControlId.RegExTextBox: return regexTextBox;
			case ControlId.SampleLogTextBox: return sampleLogTextBox;
			case ControlId.ReHelpLabel: return reHelpLabel;
			case ControlId.EmptyReLabel: return emptyReLabel;
			case ControlId.MatchesCountLabel: return matchesCountLabel;
			case ControlId.PerfValueLabel: return perfRatingLabel;
			case ControlId.LegendList: return legendContainer;
			case ControlId.LegendLabel: return legendLabel;
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
