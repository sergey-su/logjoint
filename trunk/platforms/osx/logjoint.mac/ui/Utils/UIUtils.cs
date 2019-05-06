using System;
using AppKit;
using LogJoint.Drawing;
using Foundation;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI
{
	public static class UIUtils
	{
		public static void MoveToPlaceholder(this NSView customControlView, NSView placeholder)
		{
			placeholder.AddSubview (customControlView);
			var placeholderSize = placeholder.Frame.Size;
			customControlView.Frame = new CoreGraphics.CGRect(0, 0, placeholderSize.Width, placeholderSize.Height);
			customControlView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		}

		public static T EnsureCreated<T>(this T view) where T: NSResponder
		{
			return view;
		}

		public static bool GetBoolValue(this NSButton checkbox) => checkbox.State == NSCellStateValue.On;

		public static void SetBoolValue(this NSButton checkbox, bool value)
		{
			checkbox.State = value ? NSCellStateValue.On : NSCellStateValue.Off;
		}

		public static void InvalidateCursorRects(this NSView view)
		{
			if (view.Window != null)
				view.Window.InvalidateCursorRectsForView(view);
		}

		public static PointF GetEventLocation(this NSView view, NSEvent e)
		{
			return view.ConvertPointFromView(e.LocationInWindow, null).ToPointF();
		}

		public static RectangleF FocusedItemMarkFrame
		{
			get { return new RectangleF(0f, -3.5f, 3.5f, 7f); }
		}

		public static int? GetChildViewLevel(this NSView parent, NSView viewToTest)
		{
			for (int ret = 0;; ++ret)
			{
				if (viewToTest == null)
					return null;
				if (viewToTest == parent)
					return ret;
				viewToTest = viewToTest.Superview;
			}
		}

		public static void DrawDebugLine(Graphics g, float x, float y)
		{
			g.DrawLine(red, new PointF(x, y - 5), new PointF(x, y + 5));
		}

		public static void DrawFocusedItemMark(Graphics g, float x, float y, bool drawOuterFrame = false)
		{
			if (drawOuterFrame)
			{
				g.FillPolygon(white, MakeFocusedItemMarkPoints(x - 1f, y, new SizeF(
					FocusedItemMarkFrame.Size.Width + 2.3f, FocusedItemMarkFrame.Size.Height + 4.4f)));
			}
			g.FillPolygon(blue, MakeFocusedItemMarkPoints(x, y, FocusedItemMarkFrame.Size));
		}

		static PointF[] MakeFocusedItemMarkPoints(float x, float y, SizeF sz)
		{
			var focusedItemMarkPoints = new PointF[]
			{
				new PointF(x, y-sz.Height/2),
				new PointF(x+sz.Width, y),
				new PointF(x, y+sz.Height/2)
			};
			return focusedItemMarkPoints;
		}

		public static void DrawBookmark(Graphics g, float x, float y)
		{
			// todo: stub. impl properly.
			g.FillRoundRectangle(blue, new RectangleF(x, y - 3, 8, 6), 2);
		}

		public static IEnumerable<int> GetSelectedIndices(this NSTableView outlineView)
		{
			return Enumerable.Range(0, (int)outlineView.RowCount)
				             .Where(i => outlineView.IsRowSelected(i));
		}

		public static IEnumerable<NSObject> GetSelectedItems(this NSOutlineView outlineView)
		{
			return GetSelectedIndices(outlineView)
	             .Select(i => outlineView.ItemAtRow(i));
		}
		                                               

		public static void SelectAndScrollInView<Item>(NSOutlineView treeView, Item[] items,
			Func<Item, Item> parentGetter) where Item: NSObject
		{
			var rows = new List<uint> ();
			foreach (var item in items) {
				var rowIdx = treeView.RowForItem (item);
				if (rowIdx < 0) {
					var stack = new Stack<Item>();
					for (var i = parentGetter(item); i != null; i = parentGetter(i))
						stack.Push(i);
					while (stack.Count > 0)
						treeView.ExpandItem (stack.Pop());
					rowIdx = treeView.RowForItem (item);
				}
				if (rowIdx >= 0)
					rows.Add ((uint)rowIdx);
			}
			treeView.SelectRows (
				NSIndexSet.FromArray (rows.ToArray()),
				byExtendingSelection: false
			);
			if (rows.Count > 0)
				treeView.ScrollRowToVisible((nint)rows[0]);
		}

		public static void AutoSizeColumn(this NSTableView table, int columnIdx)
		{
			nfloat width = 0;
			for (nint rowIdx = 0; rowIdx < table.RowCount; ++rowIdx)
			{
				var view = table.GetView(columnIdx, rowIdx, makeIfNecessary: true);
				if (view == null)
					continue;
				var w = view.IntrinsicContentSize.Width;
				if (w > width)
					width = w;
			}
			table.TableColumns()[columnIdx].Width = width;
		}

		public class SimpleTableDataSource<T>: NSTableViewDataSource
		{
			public List<T> Items = new List<T>();

			public override nint GetRowCount (NSTableView tableView) => Items.Count;
		};

		public static NSImage GetNamedTemplateImage (string name)
		{
			var img = NSImage.ImageNamed (name);
			img.Template = true;
			return img;
		}

		[Register("ReadonlyFormatter")]
		public class ReadonlyFormatter: NSFormatter
		{
			public override string StringFor (NSObject value)
			{
				return value?.ToString() ?? "";
			}

			public override bool GetObjectValue (out NSObject obj, string str, out NSString error)
			{
				obj = new NSString(str);
				error = null;
				return true;
			}

			[Export("isPartialStringValid:proposedSelectedRange:originalString:originalSelectedRange:errorDescription:")]
			public bool IsPartialStringValid(ref NSString partialString, ref NSRange proposedSelectedRange, 
			                                 NSString originalString, NSRange originalRange, ref NSString error)
			{
				proposedSelectedRange = new NSRange();
				error = null;
				return false;
			}
		};

		static Brush blue = new Brush(Color.Blue);
		static Brush white = new Brush(Color.White);
		static Pen red = new Pen(Color.Red, 1);
	}
}

