using System;
using System.Linq;
using AppKit;
using Foundation;

namespace LogJoint.UI
{
	[Register ("NSDragDropTextField")]
	public class NSDragDropTextField: NSTextField
	{

		public NSDragDropTextField(IntPtr handle)
			: base(handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public NSDragDropTextField(NSCoder coder)
			: base(coder)
		{
		}

		public NSDragDropTextField ()
		{
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			this.RegisterForDraggedTypes(new [] { NSPasteboard.NSFilenamesType.ToString() });
		}

		public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
		{
			if (sender.DraggingPasteboard.Types.Any(t => t == NSPasteboard.NSFilenamesType.ToString())) {
				return NSDragOperation.Copy;
			}
			return NSDragOperation.None;
		}

		static string[] GetItemsForType(NSPasteboard pboard, NSString type)
		{
			var items = NSArray.FromArray<NSString>((NSArray)pboard.GetPropertyListForType(type.ToString()));
			return items.Select(i => i.ToString()).ToArray();
		}

		public override bool PerformDragOperation(NSDraggingInfo sender)
		{
			var pboard = sender.DraggingPasteboard;
			if (pboard.Types.Contains(NSPasteboard.NSFilenamesType.ToString())) {
				var fname = GetItemsForType(pboard, NSPasteboard.NSFilenamesType).FirstOrDefault();
				if (fname != null)
					this.StringValue = fname;
			}
			return true;
		}
	};

}
