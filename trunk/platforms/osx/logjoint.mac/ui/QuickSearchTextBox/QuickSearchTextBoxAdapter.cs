
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
		}

		void IView.ReceiveInputFocus()
		{
		}

		void IView.ResetQuickSearchTimer(int due)
		{
		}

		string IView.Text
		{
			get
			{
				return "";
			}
			set
			{
			}
		}

		IViewEvents viewEvents;
	}
}

