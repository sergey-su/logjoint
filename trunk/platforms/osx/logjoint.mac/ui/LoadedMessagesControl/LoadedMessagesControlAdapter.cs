using System;
using System.Linq;
using Foundation;
using LogJoint.UI.Presenters.LoadedMessages;
using AppKit;
using LogJoint.Settings;

namespace LogJoint.UI
{
	public partial class LoadedMessagesControlAdapter: NSViewController, IView
	{
		LogViewerControlAdapter logViewerControlAdapter;
		IViewEvents viewEvents;

		public LoadedMessagesControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public LoadedMessagesControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		// Call to load from the XIB/NIB file
		public LoadedMessagesControlAdapter()
			: base("LoadedMessagesControl", NSBundle.MainBundle)
		{
			Initialize();
		}

		void Initialize()
		{
			logViewerControlAdapter = new LogViewerControlAdapter();
		}
			
		public override void AwakeFromNib()
		{
			logViewerControlAdapter.View.MoveToPlaceholder(logViewerPlaceholder);

			rawViewButton.ToolTip = "Toggle raw log view";
			toggleBookmarkButton.ToolTip = "Toggle bookmark";
			coloringButton.ToolTip = "Log coloring";

			Action<int, string, string, int> initItem = (itemIndex, title, tooltop, tag) =>
			{
				var item = coloringButton.ItemAtIndex(itemIndex);
				item.Title = title;
				item.ToolTip = tooltop;
				item.Tag = tag;
			};
			initItem(0, "Threads", "Log messages with different threads to have different background color", (int)Appearance.ColoringMode.Threads);
			initItem(1, "Sources", "Log messages from different log sources to have different background color", (int)Appearance.ColoringMode.Sources);
			initItem(2, "None", "White background for all log messages", (int)Appearance.ColoringMode.None);
		}

		void IView.SetEventsHandler(IViewEvents presenter)
		{
			this.viewEvents = presenter;
		}

		void IView.SetRawViewButtonState(bool visible, bool checked_)
		{
			rawViewButton.Hidden = !visible;
			rawViewButton.State = checked_ ? NSCellStateValue.On : NSCellStateValue.Off;
		}

		void IView.SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked)
		{
			Appearance.ColoringMode mode;
			if (noColoringChecked)
				mode = Appearance.ColoringMode.None;
			else if (sourcesColoringChecked)
				mode = Appearance.ColoringMode.Sources;
			else
				mode = Appearance.ColoringMode.Threads;
			if ((int)mode == coloringButton.SelectedItem.Tag)
				return;
			coloringButton.SelectItem(coloringButton.Items().FirstOrDefault(i => i.Tag == (int)mode));
		}

		void IView.SetNavigationProgressIndicatorVisibility(bool value)
		{
			navigationProgressIndicator.Hidden = !value;
		}

		LogJoint.UI.Presenters.LogViewer.IView IView.MessagesView
		{
			get
			{
				return logViewerControlAdapter;
			}
		}

		partial void OnRawViewButtonClicked (NSObject sender)
		{
			viewEvents.OnToggleRawView();
		}

		partial void OnToggleBookmarkButtonClicked (NSObject sender)
		{
			viewEvents.OnToggleBookmark();
		}

		partial void OnColoringButtonClicked (NSObject sender)
		{
			viewEvents.OnColoringButtonClicked((Appearance.ColoringMode) (int)coloringButton.SelectedItem.Tag);
		}
	}
}

