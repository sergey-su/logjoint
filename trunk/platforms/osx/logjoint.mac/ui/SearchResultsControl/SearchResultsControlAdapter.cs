
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SearchResult;

namespace LogJoint.UI
{
	public partial class SearchResultsControlAdapter : NSViewController, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		IViewEvents viewEvents;

		#region Constructors

		// Called when created from unmanaged code
		public SearchResultsControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SearchResultsControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public SearchResultsControlAdapter()
			: base("SearchResultsControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new SearchResultsControl View
		{
			get
			{
				return (SearchResultsControl)base.View;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			logViewerControlAdapter = new LogViewerControlAdapter();
			logViewerControlAdapter.View.MoveToPlaceholder(this.logViewerPlaceholder);
		}


		void IView.SetEventsHandler(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetSearchResultText(string value)
		{
			searchResultLabel.StringValue = value;
		}

		void IView.SetSearchStatusText(string value)
		{
			searchStatusLabel.StringValue = value;
		}

		void IView.SetSearchCompletionPercentage(int value)
		{
			searchProgress.DoubleValue = (double)value / 100d;
		}

		void IView.SetSearchProgressBarVisiblity(bool value)
		{
			searchProgress.Hidden = !value;
		}

		void IView.SetSearchStatusLabelVisibility(bool value)
		{
			searchStatusLabel.Hidden = !value;
		}

		void IView.FocusMessagesView()
		{
			logViewerControlAdapter.View.BecomeFirstResponder();
		}

		Presenters.LogViewer.IView IView.MessagesView
		{
			get { return logViewerControlAdapter; }
		}

		bool IView.IsMessagesViewFocused
		{
			get
			{
				return logViewerControlAdapter.isFocused;
			}
		}
	}
}

