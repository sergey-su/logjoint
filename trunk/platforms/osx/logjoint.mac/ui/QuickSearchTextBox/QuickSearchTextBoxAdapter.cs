using System;
using System.Collections.Generic;
using Foundation;
using AppKit;
using CoreGraphics;
using LogJoint.UI.Presenters.QuickSearchTextBox;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class QuickSearchTextBoxAdapter : AppKit.NSViewController, IView
	{
		internal IViewModel viewModel;
		SearchSuggestionsListController suggestions;
		RestrictingFormatter formatter;
		ISubscription subscription;
			
		#region Constructors

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public QuickSearchTextBoxAdapter(NSCoder coder)
			: base(coder)
		{
		}

		public QuickSearchTextBoxAdapter()
		{
			NSBundle.LoadNib ("QuickSearchTextBox", this);
		}

		#endregion

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				subscription.Dispose ();
			}
			base.Dispose (disposing);
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			TextBox.owner = this;
			TextBox.Delegate = new Delegate() { owner = this };
			TextBox.Formatter = formatter = new RestrictingFormatter();
			SetListAvailability(false);
		}

		public QuickSearchTextBox TextBox => searchField;

		void IView.SetViewModel (IViewModel viewModel) {
			View.EnsureCreated ();

			this.viewModel = viewModel;

			var updateListAvailability = Updaters.Create (
				() => viewModel.SuggestionsListAvailable, SetListAvailability);
			var updateListVisibility = Updaters.Create (
				() => viewModel.SuggestionsListVisibile, SetListVisibility);
			var updateRestriction = Updaters.Create (
				() => viewModel.TextEditingRestricted, RestrictTextEditing);
			var listItemsUpdater = Updaters.Create (
				() => viewModel.SuggestionsListContentVersion,
				_ => SetListItems (viewModel.SuggestionsListItems));
			var listSelectionUpdater = Updaters.Create (
				() => viewModel.SelectedSuggestionsListItem,
				value => SetListSelectedItem (value.GetValueOrDefault ()));
			var updateText = Updaters.Create (
				() => viewModel.Text,
				value => {
					if (TextBox.StringValue != value)
						TextBox.StringValue = value;
				}
			);

			subscription = viewModel.ChangeNotification.CreateSubscription (() => {
				updateListAvailability ();
				updateListVisibility ();
				updateRestriction ();
				listItemsUpdater ();
				listSelectionUpdater ();
				updateText ();
			});
		}

		void IView.SelectEnd()
		{
			if (TextBox.CurrentEditor != null)
				TextBox.CurrentEditor.SelectedRange = new NSRange(TextBox.StringValue.Length, 0);
		}

		void IView.SelectAll()
		{
			TextBox.CurrentEditor?.SelectAll(this);
		}

		async void IView.ReceiveInputFocus()
		{
			if (View.Window == null)
				await Task.Yield();
			if (View.Window != null)
				View.Window.MakeFirstResponder(TextBox);
		}

		void SetListVisibility(bool value)
		{
			if (value)
				EnsureListCreated();
			if (suggestions != null)
				suggestions.View.Hidden = !value;
			dropDownButton.State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			dropDownButton.ToolTip = value ? "Hide suggestions ⌘↑" : "Display suggestions ⌘↓";
		}

		void SetListAvailability(bool value)
		{
			dropDownButton.Hidden = !value;
			trailingConstraint.Constant = !value ? 0 : 26;
		}

		void RestrictTextEditing(bool restrict)
		{
			formatter.RestrictionEnabled = restrict;
		}

		void SetListItems(IReadOnlyList<ISuggestionsListItem> items)
		{
			if (!viewModel.SuggestionsListVisibile)
				return;
			EnsureListCreated ();
			suggestions.SetListItems(items);
		}

		void SetListSelectedItem(int index)
		{
			if (!viewModel.SuggestionsListVisibile)
				return;
			EnsureListCreated ();
			suggestions.SetListSelectedItem(index);
		}

		void EnsureListCreated()
		{
			if (suggestions != null)
				return;
			
			suggestions = new SearchSuggestionsListController() { owner = this };

			var suggestionsParent = TextBox.Window.ContentView;
			suggestionsParent.AddSubview (suggestions.View);
			suggestions.View.TranslatesAutoresizingMaskIntoConstraints = false;
			suggestions.View.Hidden = true;

			Action<NSLayoutAttribute, nfloat, nfloat> createConstraint = (attr, constant, multiplier) =>
			{
				var constr = NSLayoutConstraint.Create(
					suggestions.View, attr, NSLayoutRelation.Equal,
					suggestionsParent, attr, multiplier, constant);
				suggestionsParent.AddConstraint(constr);
			};

			var parentRect = suggestionsParent.Frame;
			var edtrRect = suggestionsParent.ConvertRectFromView(
				new CGRect(new CGPoint(), TextBox.Frame.Size), TextBox);
			createConstraint(NSLayoutAttribute.Top, parentRect.Height - edtrRect.Top, 1);
			createConstraint(NSLayoutAttribute.Leading, edtrRect.Left, 1);
			createConstraint(NSLayoutAttribute.Trailing, -(parentRect.Width - edtrRect.Right), 1);
			createConstraint(NSLayoutAttribute.Height, 0, 0.7f);
		}

		partial void OnSearchAction (NSObject sender)
		{
			viewModel.OnKeyDown(Key.Enter);
		}

		partial void dropDownButtonClicked (Foundation.NSObject sender)
		{
			viewModel.OnDropDownButtonClicked();
		}

		class Delegate: NSSearchFieldDelegate
		{
			public QuickSearchTextBoxAdapter owner;

			[Export("controlTextDidChange:")]
			void TextDidChange(NSObject _)
			{
				owner.viewModel.OnChangeText(owner.TextBox.StringValue);
			}

			[Export("controlTextDidEndEditing:")]
			void DidEndEditing(NSNotification evt)
			{
				var textMovement = ((NSNumber)evt.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
				if (textMovement == (nint)(long)NSTextMovement.Return)
				{
					if ((NSEvent.CurrentModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
						owner.viewModel.OnKeyDown(Key.EnterWithReverseSearchModifier);
					else
						owner.viewModel.OnKeyDown(Key.Enter);
					return;
				}
				var focusTakenOverBy = evt.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
				if (focusTakenOverBy != this && 
				    !(owner.suggestions != null && focusTakenOverBy == owner.suggestions.ListView)) 
				{
					owner.viewModel.OnLostFocus();
					return;
				}
			}
		};

		[Register("RestrictingFormatter")]
		class RestrictingFormatter: NSFormatter
		{
			public bool RestrictionEnabled;

			public override string StringFor (NSObject value)
			{
				return value?.ToString() ?? "";
			}

			public override bool GetObjectValue (out NSObject obj, string str, out NSString error)
			{
				obj = new NSString(str);
				error = null;
				return true;
			}

			[Export("isPartialStringValid:proposedSelectedRange:originalString:originalSelectedRange:errorDescription:")]
			public bool IsPartialStringValid(ref NSString partialString, ref NSRange proposedSelectedRange, 
				NSString originalString, NSRange originalRange, ref NSString error)
			{
				proposedSelectedRange = new NSRange();
				error = null;

				if (!RestrictionEnabled)
				{
					return true;
				}

				if (partialString == "")
				{
					return true;
				}

				return false;
			}
		};
	}
}

