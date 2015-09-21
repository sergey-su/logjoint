using System;
using MonoMac.Foundation;

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSObject
	{
		[Export("view")]
		public LogViewerControl View { get; set;}

		public LogViewerControlAdapter()
		{
			NSBundle.LoadNib ("LogViewerControl", this);
		}
	}
}

