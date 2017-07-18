
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using LogJoint.UI.Presenters.QuickSearchTextBox;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class QuickSearchTextBoxAdapter : AppKit.NSViewController, IView
	{
		internal IViewEvents viewEvents;
		SearchSuggestionsListController suggestions;
			
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

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			TextBox.owner = this;
			TextBox.Delegate = new Delegate() { owner = this };
			((IView)this).SetListAvailability(false);
		}

		public QuickSearchTextBox TextBox => searchField;

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SelectEnd()
		{
			if (TextBox.CurrentEditor != null)
				TextBox.CurrentEditor.SelectedRange = new NSRange(TextBox.StringValue.Length, 0);
		}

		async void IView.ReceiveInputFocus()
		{
			if (View.Window == null)
				await Task.Yield();
			if (View.Window != null)
				View.Window.MakeFirstResponder(TextBox);
		}

		void IView.ResetQuickSearchTimer(int due)
		{
			// timer functionality is implemented natively by Cocoa control
		}

		void IView.SetListVisibility(bool value)
		{
			if (value)
				EnsureListCreated();
			if (suggestions != null)
				suggestions.View.Hidden = !value;
			dropDownButton.State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			dropDownButton.ToolTip = value ? "Hide suggestions ⌘↑" : "Display suggestions ⌘↓";
		}

		void IView.SetListAvailability(bool value)
		{
			dropDownButton.Hidden = !value;
			trailingConstraint.Constant = !value ? 0 : 26;
		}

		string IView.Text
		{
			get { return TextBox.StringValue; }
			set { TextBox.StringValue = value; }
		}

		void IView.SetListItems(List<ViewListItem> items)
		{
			EnsureListCreated();
			suggestions.SetListItems(items);
		}

		void IView.SetListSelectedItem(int index)
		{
			EnsureListCreated();
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

		class Delegate: NSSearchFieldDelegate
		{
			public QuickSearchTextBoxAdapter owner;

			[Export("controlTextDidChange:")]
			void TextDidChange(NSObject _)
			{
				owner.viewEvents.OnTextChanged();
			}

			[Export("controlTextDidEndEditing:")]
			void DidEndEditing(NSNotification evt)
			{
				var textMovement = ((NSNumber)evt.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
				if (textMovement == (nint)(long)NSTextMovement.Return)
				{
					owner.viewEvents.OnKeyDown(Key.Enter);
					return;
				}
				var focusTakenOverBy = evt.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
				if (focusTakenOverBy != this && 
				    !(owner.suggestions != null && focusTakenOverBy == owner.suggestions.ListView)) 
				{
					owner.viewEvents.OnLostFocus();
					return;
				}
			}
		};

		partial void OnSearchAction (NSObject sender)
		{
			viewEvents.OnQuickSearchTimerTriggered();
		}

		partial void dropDownButtonClicked (Foundation.NSObject sender)
		{
			viewEvents.OnDropDownButtonClicked();
		}
	}
}

