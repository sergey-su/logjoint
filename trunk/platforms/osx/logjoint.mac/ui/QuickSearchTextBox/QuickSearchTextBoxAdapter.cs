
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.UI
{
	public partial class QuickSearchTextBoxAdapter : MonoMac.AppKit.NSViewController, IView
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

		void IView.ReceiveInputFocus()
		{
			View.BecomeFirstResponder();
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

		class Delegate: NSTextFieldDelegate
		{
			public QuickSearchTextBoxAdapter owner;

			[Export("controlTextDidChange:")]
			void TextDidChange()
			{
				owner.viewEvents.OnTextChanged();
			}

			[Export("controlTextDidEndEditing:")]
			void DidEndEditing()
			{
				owner.viewEvents.OnEnterPressed();
			}
		};

		partial void OnSearchAction (NSObject sender)
		{
			viewEvents.OnQuickSearchTimerTriggered();
		}


		IViewEvents viewEvents;
	}
}

