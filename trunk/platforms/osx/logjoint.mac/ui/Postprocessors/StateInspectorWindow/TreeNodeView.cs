using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Drawing;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeNodeView: NSView
	{
		public StateInspectorWindowController owner;
		public Node node;

		public void Update(Node node)
		{
			this.node = node;
			this.NeedsDisplay = true;
		}

		public override void DrawRect (RectangleF dirtyRect)
		{
			base.DrawRect (dirtyRect);

			var mainText = new NSString (node.text);
			var bounds = this.Bounds;
			bool isSelected = owner.TreeView.IsRowSelected (owner.TreeView.RowForItem (node));

			var mainTextAttrs = new NSMutableDictionary ();
			var mainTextPara = new NSMutableParagraphStyle ();
			mainTextPara.LineBreakMode = NSLineBreakMode.TruncatingTail;
			mainTextPara.TighteningFactorForTruncation = 0;
			mainTextAttrs.Add (NSAttributedString.ParagraphStyleAttributeName, mainTextPara);
			if (isSelected)
				mainTextAttrs.Add (NSAttributedString.ForegroundColorAttributeName, NSColor.White);
			else
				mainTextAttrs.Add (NSAttributedString.ForegroundColorAttributeName, NSColor.Black);

			var mainTextSz = mainText.StringSize (mainTextAttrs);
			float nodeTextAndPrimaryPropHorzSpacing = 8;
			float spaceAvailableForDefaultPropValue = 
				bounds.Width - mainTextSz.Width - nodeTextAndPrimaryPropHorzSpacing;

			var paintInfo = owner.EventsHandler.OnPaintNode(node.ToNodeInfo(), spaceAvailableForDefaultPropValue > 30);
			if (paintInfo.PrimaryPropValue != null)
			{
				RectangleF r = new RectangleF(
					bounds.Right - spaceAvailableForDefaultPropValue,
					bounds.Y,
					spaceAvailableForDefaultPropValue,
					bounds.Height);

				// todo: move dict to a static field
				var dict = new NSMutableDictionary ();
				var para = new NSMutableParagraphStyle ();
				para.Alignment = NSTextAlignment.Right;
				para.LineBreakMode = NSLineBreakMode.TruncatingTail;
				para.TighteningFactorForTruncation = 0;
				dict.Add (NSAttributedString.ParagraphStyleAttributeName, para);
				if (isSelected)
					dict.Add (NSAttributedString.ForegroundColorAttributeName, NSColor.FromDeviceRgba(0.9f, 0.9f, 0.9f, 1f));
				else
					dict.Add (NSAttributedString.ForegroundColorAttributeName, NSColor.Gray);

				new NSString (paintInfo.PrimaryPropValue).DrawString (r, dict);
			}

			mainText.DrawString (bounds, mainTextAttrs);
		}
	};

}

