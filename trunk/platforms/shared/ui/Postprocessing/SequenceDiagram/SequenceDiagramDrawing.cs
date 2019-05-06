using System;
using System.Linq;
using LJD = LogJoint.Drawing;
using LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer;
using LogJoint.Drawing;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	public class Resources
	{
		public LJD.Pen RequestPen, ResponsePen;
		public LJD.Pen HighlightedRequestPen, HighlightedResponsePen;
		public LJD.Color SelectedLineColor;
		public LJD.Brush SelectedLineBrush;
		public LJD.Brush ControlBackgroundBrush;
		public LJD.StringFormat RoleCaptionFormat;
		public LJD.Brush UserActionBrush;
		public LJD.Brush StateChangeBrush;
		public LJD.Pen UserActionFramePen;
		public LJD.Brush BookmarkArrowBackgroundBrush;
		public LJD.Pen BookmarkArrowPen;
		public LJD.Font Font;
		public LJD.Font UnderlinedFont;
		public LJD.Pen RolePen;
		public LJD.Pen CaptionRectPen;
		public LJD.Brush LinkCaptionBrush;
		public LJD.Brush NoLinkCaptionBrush;
		public LJD.Image FocusedMsgSlaveVert;
		public LJD.Pen ExecutionOccurrencePen, HighlightedExecutionOccurrencePen;
		public LJD.Brush NormalExecutionOccurrenceBrush, NormalHighlightedExecutionOccurrenceBrush;
		public LJD.Brush ActivityExecutionOccurrenceBrush, ActivityHighlightedExecutionOccurrenceBrush;
		public LJD.Brush NormalArrowTextBrush;
		public LJD.Brush ErrorArrowTextBrush;
		public LJD.StringFormat ArrowTextFormat;
		public LJD.StringFormat UserActionTextFormat;
		public LJD.Image UserActionImage;

		public LJD.Image FocusedMessageImage, BookmarkImage;
		public LJD.StringFormat TimeDeltaFormat;
		public PointF[] ArrowEndShapePoints;

		public readonly float DpiScale;

		public Resources(string fontFamily, float fontSize, float dpiSensitivePensScale = 1f)
		{
			this.UserActionBrush = new LJD.Brush(Color.LightSalmon);
			this.StateChangeBrush = new LJD.Brush(Color.FromArgb(0xff, Color.FromArgb(0xC8F6C8)));
			this.UserActionFramePen = new LJD.Pen(Color.Gray, 1);
			this.BookmarkArrowBackgroundBrush = new LJD.Brush(Color.FromArgb(0xff, Color.FromArgb(0xE3EDFF)));
			this.BookmarkArrowPen = new LJD.Pen(Color.Gray, 1);
			this.SelectedLineColor = Color.FromArgb(187, 196, 221);
			this.SelectedLineBrush = new LJD.Brush(SelectedLineColor);
			this.ControlBackgroundBrush = new LJD.Brush(Color.White);
			this.Font = new LJD.Font(fontFamily, fontSize);
			this.UnderlinedFont = new LJD.Font(fontFamily, fontSize, FontStyle.Underline);
			this.RolePen = new LJD.Pen(Color.Black, 2 * dpiSensitivePensScale);
			this.RoleCaptionFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Center, LineBreakMode.WrapChars);
			float normalLineWidth = 1 * dpiSensitivePensScale;
			this.RequestPen = new LJD.Pen(Color.Black, normalLineWidth);
			this.ResponsePen = new LJD.Pen(Color.Black, normalLineWidth, dashPattern: new float[] { 5, 2 });
			float highlightedLineWidth = (float)Math.Ceiling (2f * dpiSensitivePensScale);
			this.HighlightedRequestPen = new LJD.Pen(Color.Black, highlightedLineWidth);
			this.HighlightedResponsePen = new LJD.Pen(Color.Black, highlightedLineWidth, dashPattern: new float[] { 5 * normalLineWidth / highlightedLineWidth, 2 * normalLineWidth / highlightedLineWidth });
			this.CaptionRectPen = new LJD.Pen(Color.Black, 1);
			this.LinkCaptionBrush = new LJD.Brush(Color.Blue);
			this.NoLinkCaptionBrush = new LJD.Brush(Color.Black);
			this.ExecutionOccurrencePen = new LJD.Pen(Color.Black, 1);
			this.HighlightedExecutionOccurrencePen = new LJD.Pen(Color.Black, highlightedLineWidth);
			this.NormalExecutionOccurrenceBrush = new LJD.Brush(Color.LightGray);
			this.NormalHighlightedExecutionOccurrenceBrush = new LJD.Brush(Color.DarkGray);
			this.ActivityExecutionOccurrenceBrush = new LJD.Brush(Color.LightBlue);
			this.ActivityHighlightedExecutionOccurrenceBrush = this.ActivityExecutionOccurrenceBrush;//new LJD.Brush(Color.FromArgb(0xff, Color.FromArgb(0x8DB8C6)));
			this.NormalArrowTextBrush = new LJD.Brush(Color.Black);
			this.ErrorArrowTextBrush = new LJD.Brush(Color.Red);
			this.ArrowTextFormat = new LJD.StringFormat(StringAlignment.Near, StringAlignment.Far, LineBreakMode.SingleLineEndEllipsis);
			this.UserActionTextFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Far, LineBreakMode.SingleLineEndEllipsis);
			this.TimeDeltaFormat = new LJD.StringFormat(StringAlignment.Far, StringAlignment.Center);
			this.ArrowEndShapePoints = new [] {
				new PointF (-5, -4),
				new PointF (-5, +4),
				new PointF (0, 0)
			};
			this.DpiScale = dpiSensitivePensScale;
		}
	};

	public struct RoleCaptionMetrics
	{
		public RoleDrawInfo DrawInfo;
		public Rectangle Box;
		public bool IsLink;
		public bool IsFocused;
	};

	public enum CursorKind
	{
		Pointer,
		Hand
	};

	public class DrawingUtils
	{
		readonly IViewEvents eventsHandler;
		readonly Resources resources;

		public DrawingUtils(IViewEvents eventsHandler, Resources resources)
		{
			this.eventsHandler = eventsHandler;
			this.resources = resources;
		}

		public IEnumerable<RoleCaptionMetrics> GetRoleCaptionsMetrics(LJD.Graphics g)
		{
			if (eventsHandler == null)
				yield break;

			foreach (var role in eventsHandler.OnDrawRoles())
			{
				if (role.Bounds.Width < 2)
					continue;
				var sz = g.MeasureString(role.DisplayName, resources.Font, resources.RoleCaptionFormat, role.Bounds.Size);
				float padding = 3;
				sz.Width = Math.Min(
					Math.Max(sz.Width + padding, role.Bounds.Height),
					role.Bounds.Width
				);
				yield return new RoleCaptionMetrics()
				{
					DrawInfo = role,
					Box = new Rectangle(
						role.X - (int)sz.Width / 2,
						role.Bounds.Y,
						(int)sz.Width,
						role.Bounds.Height
					),
					IsLink = role.LogSourceTrigger != null,
					IsFocused = role.ContainsFocusedMessage
				};
			}
		}

		public void DrawRoleCaptions(LJD.Graphics g)
		{
			foreach (var role in GetRoleCaptionsMetrics(g))
			{
				g.DrawRectangle(resources.CaptionRectPen, role.Box);
				g.DrawString(
					role.DrawInfo.DisplayName,
					role.IsLink ? resources.UnderlinedFont : resources.Font,
					role.IsLink ? resources.LinkCaptionBrush : resources.NoLinkCaptionBrush,
					role.Box,
					resources.RoleCaptionFormat);
				if (role.IsFocused && resources.FocusedMsgSlaveVert != null)
				{
					var img = resources.FocusedMsgSlaveVert;
					var sz = img.GetSize(width: 10f).Scale(resources.DpiScale);
					g.DrawImage(img, new RectangleF(
						role.Box.X + (role.Box.Width - sz.Width) / 2, role.Box.Bottom - sz.Height - 1,
						sz.Width, sz.Height
					));
				}
			}
		}

		public CursorKind GetRoleCaptionsCursor(LJD.Graphics g, Point pt)
		{
			foreach (var role in GetRoleCaptionsMetrics(g))
			{
				if (role.IsLink && role.Box.Contains(pt))
				{
					return CursorKind.Hand;
				}
			}
			return CursorKind.Pointer;
		}

		public void HandleRoleCaptionsMouseDown(LJD.Graphics g, Point pt)
		{
			foreach (var role in GetRoleCaptionsMetrics(g))
			{
				if (role.IsLink && role.Box.Contains(pt))
				{
					eventsHandler.OnTriggerClicked(role.DrawInfo.LogSourceTrigger);
					return;
				}
			}
		}

		public void DrawArrowsView(LJD.Graphics g, Size viewSize, Action<Rectangle> drawFocusRect)
		{
			foreach (var message in eventsHandler.OnDrawArrows())
			{
				if (!message.IsFullyVisible)
					continue;
				if (message.SelectionState != ArrowSelectionState.NotSelected)
				{
					int y = message.Y;
					int h = message.Height;
					var r = new Rectangle(
						0, y - h + 2,
						viewSize.Width, h - 2
					);
					g.FillRectangle(resources.SelectedLineBrush, r);
					if (message.SelectionState == ArrowSelectionState.FocusedSelectedArrow)
					{
						drawFocusRect(r);
					}
				}
			}

			foreach (var role in eventsHandler.OnDrawRoles())
			{
				var x = role.X;
				g.DrawLine(resources.RolePen, x, 0, x, viewSize.Height);
				foreach (var eo in role.ExecutionOccurrences)
				{
					LJD.Brush fillBrush;
					if (eo.DrawMode == ExecutionOccurrenceDrawMode.Normal)
						fillBrush = eo.IsHighlighted ? resources.NormalHighlightedExecutionOccurrenceBrush : resources.NormalExecutionOccurrenceBrush;
					else if (eo.DrawMode == ExecutionOccurrenceDrawMode.Activity)
						fillBrush = eo.IsHighlighted ? resources.ActivityHighlightedExecutionOccurrenceBrush : resources.ActivityExecutionOccurrenceBrush;
					else
						continue;
					g.FillRectangle(fillBrush, eo.Bounds);
					g.DrawRectangle(eo.IsHighlighted ? resources.HighlightedExecutionOccurrencePen : resources.ExecutionOccurrencePen, eo.Bounds);
				}
			}

			Action<string, LJD.Brush, LJD.Brush, RectangleF> drawTextHelper = (str, back, fore, textRect) =>
			{
				if (back != null)
				{
					var fillSz = g.MeasureString(str, resources.Font, resources.ArrowTextFormat, textRect.Size);
					var fillRect = new RectangleF(textRect.X, textRect.Y + 4, fillSz.Width, textRect.Height - 5);
					g.FillRectangle(back, fillRect);
				}
				g.DrawString(str, resources.Font, fore, textRect, resources.ArrowTextFormat);
			};

			Action<PointF, bool> drawArrow = (pt, leftToRight) => {
				var pts = new List<PointF>(4);
				pts.Add(pt);
				foreach (var p in resources.ArrowEndShapePoints)
					pts.Add(new PointF(pt.X + (leftToRight ? 1f : -1f) * p.X, pt.Y + p.Y));
				g.FillPolygon(resources.NormalArrowTextBrush, pts.ToArray());
			};

			foreach (var message in eventsHandler.OnDrawArrows())
			{
				int y = message.Y;
				int x1 = message.FromX;
				int x2 = message.ToX;
				int h = message.Height;
				int w = message.Width;
				int padding = message.TextPadding;
				var backBrush = message.SelectionState != ArrowSelectionState.NotSelected ? resources.SelectedLineBrush : resources.ControlBackgroundBrush;
				var textBrush = message.Color == ArrowColor.Error ? resources.ErrorArrowTextBrush : resources.NormalArrowTextBrush;

				if (message.Mode == ArrowDrawMode.Arrow
				 || message.Mode == ArrowDrawMode.DottedArrow)
				{
					var pen = !message.IsHighlighted ?
						(message.Mode == ArrowDrawMode.Arrow ? resources.RequestPen : resources.ResponsePen) :
						(message.Mode == ArrowDrawMode.Arrow ? resources.HighlightedRequestPen : resources.HighlightedResponsePen);
					if (x1 != x2)
					{
						if (message.IsFullyVisible)
						{
							drawTextHelper(
								message.DisplayName,
								backBrush,
								textBrush,
								new RectangleF(
									Math.Min(x1, x2) + padding, y - h,
									Math.Abs(x1 - x2) - padding * 2, h
								)
							);
						}

						var nonHorizontal = message.NonHorizontalDrawingData;
						if (nonHorizontal != null)
						{
							g.DrawLines(
								pen,
								new[]
								{
									new Point(x1, y),
									new Point(nonHorizontal.VerticalLineX, y),
									new Point(nonHorizontal.VerticalLineX, nonHorizontal.Y),
									new Point(nonHorizontal.ToX, nonHorizontal.Y)
								}
							);
							drawArrow (new PointF (nonHorizontal.ToX, nonHorizontal.Y), x2 > x1);
						}
						else
						{
							if (message.IsFullyVisible)
							{
								g.DrawLine(
									pen,
									x1, y,
									x2, y
								);
								drawArrow(new PointF(x2, y), x2 > x1);
							}
						}
					}
					else
					{
						var midY = y - h / 2;
						var loopW = 10;
						var loopH = 10;

						drawTextHelper(
							message.DisplayName,
							backBrush,
							textBrush,
							new RectangleF(
								x1 + loopW + padding, y - h,
								message.Width - (loopW + padding * 2), h
							)
						);

						g.DrawLines(
							pen,
							new[]
							{
								new Point(x1, midY - loopH/2),
								new Point(x1 + loopW, midY - loopH/2),
								new Point(x1 + loopW, midY + loopH/2),
								new Point(x1, midY + loopH/2),
							}
						);
						drawArrow (new PointF (x1, midY + loopH/2), false);
					}
				}
				else if (message.Mode == ArrowDrawMode.Bookmark)
				{
					if (!message.IsFullyVisible)
						continue;
					float radius = h / 4f;
					var textRect = new RectangleF(
						message.MinX - w / 2 + padding, y - h,
						(message.MaxX - message.MinX + w) - padding * 2, h
					);
					var sz = g.MeasureString("ABC", resources.Font, resources.ArrowTextFormat, textRect.Size).ToSize();
					var r = new RectangleF(textRect.X, y - sz.Height - 1, textRect.Width, sz.Height);
					r.Inflate (radius, 0);

					g.PushState();
					g.EnableAntialiasing(true);
					g.FillRoundRectangle(resources.BookmarkArrowBackgroundBrush, r, radius);
					g.DrawRoundRectangle(resources.BookmarkArrowPen, r, radius);
					g.PopState();

					SizeF q = new SizeF(3, 3).Scale(resources.DpiScale);
					if (x1 - q.Width > r.Left && x1 + q.Width < r.Right)
					{
						PointF[] pts;

						pts = new [] 
						{
							new PointF(x1 - q.Width, r.Top + r.Height/2),
							new PointF(x1 - q.Width, r.Top),
							new PointF(x1, r.Top - q.Height),
							new PointF(x1 + q.Width, r.Top),
							new PointF(x1 + q.Width, r.Top + r.Height/2),
						};
						g.FillPolygon(resources.BookmarkArrowBackgroundBrush, pts);
						g.DrawLines(resources.BookmarkArrowPen, pts.Skip(1).Take(3).ToArray());

						pts = new [] 
						{
							new PointF(x1 + q.Width, r.Bottom - r.Height/2),
							new PointF(x1 + q.Width, r.Bottom),
							new PointF(x1, r.Bottom + q.Height),
							new PointF(x1 - q.Width, r.Bottom),
							new PointF(x1 - q.Width, r.Bottom - r.Height/2),
						};
						g.FillPolygon(resources.BookmarkArrowBackgroundBrush, pts);
						g.DrawLines(resources.BookmarkArrowPen, pts.Skip(1).Take(3).ToArray());
					}

					drawTextHelper(
						message.DisplayName,
						null,
						textBrush,
						textRect
					);
				}
				else if (message.Mode == ArrowDrawMode.UserAction 
					|| message.Mode == ArrowDrawMode.StateChange 
					|| message.Mode == ArrowDrawMode.APICall
					|| message.Mode == ArrowDrawMode.ActivityLabel)
				{
					if (!message.IsFullyVisible)
						continue;
					var icon = message.Mode == ArrowDrawMode.UserAction ? resources.UserActionImage : null;
					float radius = h / 4f;
					var r = new RectangleF(
						x1 - (w - radius), y - h,
						(w - radius) * 2, h
					);
					var sz = g.MeasureString(message.DisplayName, resources.Font, resources.UserActionTextFormat, r.Size);
					var szh = sz.Height;
					var boxRect = new RectangleF(
						x1 - sz.Width / 2, y - szh - 1,
						sz.Width, szh
					);
					boxRect.Inflate(radius, 0);
					g.PushState();
					g.EnableAntialiasing(true);
					LJD.Brush backgroundBrush = null;
					LJD.Pen strokePen = resources.UserActionFramePen;
					if (message.Mode == ArrowDrawMode.UserAction || message.Mode == ArrowDrawMode.APICall)
					{
						backgroundBrush = resources.UserActionBrush;
					}
					else if (message.Mode == ArrowDrawMode.StateChange)
					{
						backgroundBrush = resources.StateChangeBrush;
					}
					else if (message.Mode == ArrowDrawMode.ActivityLabel)
					{
						backgroundBrush = resources.ActivityExecutionOccurrenceBrush;
						if (message.IsHighlighted)
							strokePen = resources.HighlightedExecutionOccurrencePen;
					}
					g.FillRoundRectangle(
						backgroundBrush,
						boxRect,
						radius
					);
					g.DrawRoundRectangle(
						strokePen,
						boxRect,
						radius
					);
					g.PopState();
					g.DrawString(message.DisplayName, resources.Font,
						textBrush, boxRect, resources.UserActionTextFormat);
					if (icon != null)
					{
						var iconsz = icon.GetSize(width: 10f).Scale(resources.DpiScale);
						g.DrawImage(
							icon,
							new RectangleF(
								(int)(boxRect.Left - iconsz.Width * 1.3f),
								(int)(boxRect.Y + (boxRect.Height - iconsz.Height) / 2),
								iconsz.Width,
								iconsz.Height
							)
						);
					}
				}
			}
		}

		public void DrawLeftPanelView(LJD.Graphics g, Point origin, Size viewSize)
		{
			var w = viewSize.Width;
			foreach (var message in eventsHandler.OnDrawArrows())
			{
				int y = origin.Y + message.Y;

				if (message.SelectionState != ArrowSelectionState.NotSelected)
				{
					g.FillRectangle(
						resources.SelectedLineBrush,
						new Rectangle(0, y - message.Height + 2, w, message.Height - 2)
					);
				}

				var focusedMessageImageSz = resources.FocusedMessageImage.GetSize(width: 4.5f);
				g.DrawString(message.Delta, resources.Font, resources.NormalArrowTextBrush,
					new RectangleF(0, y - message.Height, w - focusedMessageImageSz.Width - 4 * resources.DpiScale, message.Height),
					resources.TimeDeltaFormat);

				if (message.CurrentTimePosition != null)
				{
					var img = resources.FocusedMessageImage;
					var sz = focusedMessageImageSz.Scale(resources.DpiScale);
					g.DrawImage(img, new RectangleF(
						w - sz.Width - 3,
						y - message.Height / 2 + Math.Sign(message.CurrentTimePosition.Value) * message.Height / 2 - sz.Height / 2,
						sz.Width, sz.Height));
				}

				if (message.IsBookmarked)
				{
					var img = resources.BookmarkImage;
					var sz = img.GetSize(width: 12).Scale(resources.DpiScale);
					g.DrawImage(img, new RectangleF(
						2, y - (message.Height + sz.Height) / 2, sz.Width, sz.Height
					));
				}
			}
		}
	}
}

