using System;
using Foundation;
using LogJoint.UI.Presenters.SourcesList;
using AppKit;
using System.Collections.Generic;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	public class SourcesListItem: NSObject, IViewItem
	{
		public string text;
		public object datum;
		public bool isSelected;
		public bool? isChecked;
		public Color? color;
		public Action<SourcesListItem> updater;
		public IViewEvents viewEvents;
		public List<SourcesListItem> items = new List<SourcesListItem>();
		public SourcesListItem parent;

		void IViewItem.SetText(string value)
		{
			text = value;
			Update();
		}

		void IViewItem.SetBackColor(Color color, bool isFailureColor)
		{
			this.color = isFailureColor ? color : new Color?();
			Update();
		}

		object IViewItem.Datum 
		{
			get { return datum; }
		}

		bool IViewItem.Selected
		{
			get
			{
				return isSelected;
			}
			set
			{
				isSelected = value;
				Update();
			}
		}

		bool? IViewItem.Checked
		{
			get
			{
				return isChecked;
			}
			set
			{
				isChecked = value;
				Update();
			}
		}

		[Export("ItemChecked:")]
		public void ItemChecked(NSObject sender)
		{
			isChecked = ((NSButton)sender).State == NSCellStateValue.On;
			if (viewEvents != null)
				viewEvents.OnItemChecked(this);
		}

		void Update()
		{
			if (updater != null)
				updater(this);
		}
	}
}

