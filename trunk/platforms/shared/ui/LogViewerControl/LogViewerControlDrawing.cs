using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using SizeF = System.Drawing.SizeF;

namespace LogJoint.UI.LogViewer
{
	public class ViewDrawing
	{
		private readonly IViewModel viewModel;
		private GraphicsResources graphicsResources;
		private readonly Func<int> timeAreaSize;
		private readonly Func<(SizeF charSize, double charWidth, int lineHeight)> fontDependentData;
		private readonly Func<int> scrollPosXSelector;
		private readonly Func<int> viewWidthSelector;

		public int TimeAreaSize => timeAreaSize();
		public SizeF CharSize => fontDependentData().charSize;
		public double CharWidthDblPrecision => fontDependentData().charWidth;
		public int LineHeight => fontDependentData().lineHeight;
		public int ServiceInformationAreaSize { get; private set; }
		public float DpiScale { get; private set; }
		public int ScrollPosY => (int)(viewModel.FirstDisplayMessageScrolledLines * LineHeight);
		public int ScrollPosX => scrollPosXSelector();
		public int ViewWidth => viewWidthSelector();

		public ViewDrawing(
			IViewModel viewModel,
			GraphicsResources graphicsResources,
			float dpiScale,
			Func<int> scrollPosXSelector,
			Func<int> viewWidthSelector
		)
		{
			this.viewModel = viewModel;
			this.graphicsResources = graphicsResources;
			this.DpiScale = dpiScale;
			this.scrollPosXSelector = scrollPosXSelector;
			this.viewWidthSelector = viewWidthSelector;
			this.ServiceInformationAreaSize = (int)(25f * dpiScale);
			timeAreaSize = Selectors.Create(
				() => viewModel.TimeMaxLength,
				() => CharSize.Width,
				(maxTimeLength, charWidth) => maxTimeLength == 0 ? 0 : ((int)Math.Floor(charWidth * maxTimeLength) + 10)
			);
			fontDependentData = Selectors.Create(
				() => graphicsResources.Font,
				font =>
				{
					using (var tmp = graphicsResources.CreateGraphicsForMeasurment())
					{
						int count = 8 * 1024;
						var charSize = tmp.MeasureString(new string('0', count), font);
						var charWidth = (double)charSize.Width / (double)count;
						charSize.Width /= (float)count;
						var lineHeight = (int)Math.Floor(charSize.Height);
						return (charSize, charWidth, lineHeight);
					}
				}
			);
		}

		public Rectangle GetMessageRect(ViewLine line)
		{
			return GetMetrics(line).MessageRect;
		}

		internal Point GetTextOffset(int displayIndex)
		{
			int x = this.ServiceInformationAreaSize - ScrollPosX + TimeAreaSize;
			int y = displayIndex * LineHeight - ScrollPosY;
			return new Point(x, y);
		}

		internal ViewLineMetrics GetMetrics(ViewLine line)
		{
			Point offset = GetTextOffset(line.LineIndex);

			ViewLineMetrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				ViewWidth,
				LineHeight
			);

			m.TimePos = new Point(
				ServiceInformationAreaSize - ScrollPosX,
				m.MessageRect.Y
			);

			int charCount = line.TextLineValue.Length;

			m.OffsetTextRect = new Rectangle(
				offset.X,
				m.MessageRect.Y,
				(int)((double)charCount * CharWidthDblPrecision),
				m.MessageRect.Height
			);

			return m;
		}

		private int GetClickedCharIndex(Graphics canvas, ViewLine msg, ViewLineMetrics m, int clieckedPointX)
		{
			return ScreenPositionToMessageTextCharIndex(canvas, msg, graphicsResources.Font, graphicsResources.TextFormat,
				(int)(clieckedPointX - m.OffsetTextRect.X));
		}

		private static int ScreenPositionToMessageTextCharIndex(Graphics g,
			ViewLine msg, Font font, StringFormat format, int screenPosition)
		{
			var lineValue = msg.TextLineValue;
			int lineCharIdx = ListUtils.BinarySearch(new ListUtils.VirtualList<int>(lineValue.Length, i => i), 0, lineValue.Length, i =>
			{
				var charBounds = g.MeasureCharacterRange(lineValue, font, format, new System.Drawing.CharacterRange(i, 1));
				return ((charBounds.Left + charBounds.Right) / 2) < screenPosition;
			});
			//return (line.StartIndex + lineCharIdx) - txt.StartIndex;
			return lineCharIdx;
		}

		private IEnumerable<ViewLine> GetVisibleMessagesIterator(Rectangle viewRect)
		{
			var (begin, end) = GetVisibleMessages(viewRect);
			for (var i = begin; i < end; ++i)
				yield return viewModel.ViewLines[i];
		}

		private (int, int) GetVisibleMessages(Rectangle viewRect)
		{
			viewRect.Offset(0, ScrollPosY);

			int begin = viewRect.Y / LineHeight;
			int end = viewRect.Bottom / LineHeight;

			if ((viewRect.Bottom % LineHeight) != 0)
				++end;

			int availableLines = viewModel.ViewLines.Length;
			return (Math.Min(availableLines, begin), Math.Min(availableLines, end));
		}

		public void PaintControl(
			Graphics canvas,
			Rectangle dirtyRect,
			bool controlIsFocused,
			out int maxRight)
		{
			var darkMode = viewModel.ColorTheme == ColorThemeMode.Dark;
			bool drawViewLinesAggregaredText;
#if MONOMAC
			drawViewLinesAggregaredText = true;
#else
			drawViewLinesAggregaredText = false;
#endif
			if (darkMode)
				drawViewLinesAggregaredText = false;

			maxRight = 0;
			foreach (var vl in GetVisibleMessagesIterator(dirtyRect))
			{
				MessageDrawing.Draw(vl, graphicsResources, this, canvas, controlIsFocused, out var right, !drawViewLinesAggregaredText, darkMode);
				maxRight = Math.Max(maxRight, right);
			}

			if (drawViewLinesAggregaredText && viewModel.ViewLines.Length > 0)
			{
				canvas.DrawString(viewModel.ViewLinesAggregaredText, graphicsResources.Font, graphicsResources.DefaultForegroundBrush,
					GetMetrics(viewModel.ViewLines[0]).OffsetTextRect.Location, graphicsResources.TextFormat);
			}

			DrawFocusedMessageMark(canvas);
		}

		public void HandleMouseDown(
			Rectangle clientRectangle,
			Point pt,
			MessageMouseEventFlag flags,
			out bool captureTheMouse
		)
		{
			captureTheMouse = true;

			using (var g = graphicsResources.CreateGraphicsForMeasurment())
			foreach (var i in GetVisibleMessagesIterator(clientRectangle))
			{
				var mtx = GetMetrics(i);

				// if user clicked line area
				if (mtx.MessageRect.Contains(pt.X, pt.Y))
				{
					var lineTextPosition = GetClickedCharIndex(g, i, mtx, pt.X);
					if ((flags & MessageMouseEventFlag.DblClick) != 0)
					{
						captureTheMouse = false;
					}
					if (pt.X < ServiceInformationAreaSize)
					{
						flags |= MessageMouseEventFlag.OulineBoxesArea;
					}
					viewModel.OnMessageMouseEvent(i, lineTextPosition, flags, pt);
					break;
				}
			}
		}

		public void HandleMouseMove(
			Rectangle clientRectangle,
			Point pt,
			bool isLeftDrag,
			out CursorType newCursor
		)
		{
			newCursor = CursorType.Arrow;

			using (var g = graphicsResources.CreateGraphicsForMeasurment())
			foreach (var i in GetVisibleMessagesIterator(clientRectangle))
			{
				var mtx = GetMetrics(i);

				if (pt.Y >= mtx.MessageRect.Top && pt.Y < mtx.MessageRect.Bottom)
				{
					if (isLeftDrag)
					{
						var lineTextPosition = GetClickedCharIndex(g, i, mtx, pt.X);
						MessageMouseEventFlag flags = MessageMouseEventFlag.ShiftIsHeld
							| MessageMouseEventFlag.CapturedMouseMove;
						if (pt.X < ServiceInformationAreaSize)
							flags |= MessageMouseEventFlag.OulineBoxesArea;
						viewModel.OnMessageMouseEvent(i, lineTextPosition, flags, pt);
					}
					if (pt.X < ServiceInformationAreaSize)
						newCursor = CursorType.RightToLeftArrow;
					else if (pt.X >= GetTextOffset(0).X)
						newCursor = CursorType.IBeam;
					else
						newCursor = CursorType.Arrow;
				}
			}
		}

		void DrawFocusedMessageMark(Graphics canvas)
		{
			Image focusedMessageMark;
			SizeF focusedMessageSz;
			float markYPos;
			var slaveMessagePositionAnimationStep = 0;
			var loc = viewModel.FocusedMessageMark;
			if (loc == null)
			{
				focusedMessageMark = null;
				focusedMessageSz = new SizeF();
				markYPos = 0;
			}
			else if (loc.Length == 1)
			{
				focusedMessageMark = graphicsResources.FocusedMessageIcon;
				focusedMessageSz = focusedMessageMark.GetSize(height: 14);
				markYPos = GetTextOffset(loc[0]).Y + (LineHeight - focusedMessageSz.Height) / 2;
			}
			else
			{
				focusedMessageMark = graphicsResources.FocusedMessageIcon;
				focusedMessageSz = focusedMessageMark.GetSize(height: 9);
				float yOffset = loc[0] != loc[1] ?
					(LineHeight - focusedMessageSz.Height) / 2 : -focusedMessageSz.Height / 2;
				markYPos = GetTextOffset(loc[0]).Y + yOffset;
				slaveMessagePositionAnimationStep = loc[2];
			}
			if (focusedMessageMark != null)
			{
				canvas.PushState();
				canvas.TranslateTransform(
					ServiceInformationAreaSize - focusedMessageSz.Width / 2 + 1,
					markYPos + focusedMessageSz.Height / 2);
				if (slaveMessagePositionAnimationStep > 0)
				{
					focusedMessageSz = focusedMessageMark.GetSize(height: 10);
					var factors = new float[] { .81f, 1f, 0.9f, .72f, .54f, .36f, .18f, .09f };
					float factor = 1f + 1.4f * factors[slaveMessagePositionAnimationStep - 1];
					canvas.ScaleTransform(factor, factor);
				}
				canvas.DrawImage(
					focusedMessageMark, new RectangleF(
						-focusedMessageSz.Width / 2,
						-focusedMessageSz.Height / 2,
						focusedMessageSz.Width,
						focusedMessageSz.Height
					));
				canvas.PopState();
			}
		}
	};

	internal struct ViewLineMetrics
	{
		public Rectangle MessageRect;
		public Point TimePos;
		public Rectangle OffsetTextRect;
	};

	public enum CursorType
	{
		Arrow,
		RightToLeftArrow,
		IBeam
	};
}
