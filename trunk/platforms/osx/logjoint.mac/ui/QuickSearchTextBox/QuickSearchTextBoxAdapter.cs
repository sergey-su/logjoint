
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.QuickSearchTextBox;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class QuickSearchTextBoxAdapter : AppKit.NSViewController, IView
	{
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
			View.Delegate = new Delegate() { owner = this };
		}


		//strongly typed view accessor
		public new NSSearchField View
		{
			get
			{
				return (NSSearchField)base.View;
			}
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SelectEnd()
		{
			if (View.CurrentEditor != null)
				View.CurrentEditor.SelectedRange = new NSRange(View.StringValue.Length, 0);
		}

		async void IView.ReceiveInputFocus()
		{
			if (View.Window == null)
				await Task.Yield();
			if (View.Window != null)
				View.Window.MakeFirstResponder(View);
		}

		void IView.ResetQuickSearchTimer(int due)
		{
			// todo
		}

		string IView.Text
		{
			get { return View.StringValue; }
			set { View.StringValue = value; }
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
					owner.viewEvents.OnEnterPressed();
				}
			}
		};

		partial void OnSearchAction (NSObject sender)
		{
			viewEvents.OnQuickSearchTimerTriggered();
		}


		IViewEvents viewEvents;
	}
}

