using System.Drawing;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public class GraphicsResources
	{
		public readonly LJD.Font ActivitesCaptionsFont;
		public readonly LJD.Brush ActivitesCaptionsBrush;

		public readonly LJD.Font ActionCaptionFont, RulerMarkFont;
		public readonly LJD.Image UserIcon, APIIcon, BookmarkIcon;
		public readonly LJD.Brush SelectedLineBrush, RulerMarkBrush;
		public readonly LJD.Pen RulerLinePen;

		public readonly LJD.Brush ProcedureBrush;
		public readonly LJD.Brush LifetimeBrush;
		public readonly LJD.Brush NetworkMessageBrush;
		public readonly LJD.Brush UnknownActivityBrush;
		public readonly LJD.Pen ActivitiesTopBoundPen, MilestonePen, ActivityBarBoundsPen, ActivitiesConnectorPen;
		public readonly LJD.Brush[] PhaseBrushes;

		public readonly LJD.Pen UserEventPen;
		public readonly LJD.Brush EventRectBrush;
		public readonly LJD.Pen EventRectPen;
		public readonly LJD.Brush EventCaptionBrush;
		public readonly LJD.Font EventCaptionFont;
		public readonly LJD.StringFormat EventCaptionStringFormat;

		public readonly LJD.Pen BookmarkPen;

		public readonly LJD.Pen FocusedMessagePen;
		public readonly LJD.Image FocusedMessageLineTop;

		public readonly LJD.Pen MeasurerPen;
		public readonly LJD.Font MeasurerTextFont;
		public readonly LJD.Brush MeasurerTextBrush;
		public readonly LJD.Brush MeasurerTextBoxBrush;
		public readonly LJD.Pen MeasurerTextBoxPen;
		public readonly LJD.StringFormat MeasurerTextFormat;

		public readonly LJD.Brush NavigationPanel_InvisibleBackground;
		public readonly LJD.Brush NavigationPanel_VisibleBackground;
		public readonly LJD.Brush SystemControlBrush;
		public readonly LJD.Pen VisibleRangePen;

		public readonly LJD.Pen FoldingSignPen;

		public GraphicsResources(
			string fontName,
			float activitesCaptionsFontSize,
			float actionCaptionsFontSize,
			float rulerMarkFontSize,
			LJD.Image userIcon,
			LJD.Image apiIcon,
			LJD.Image bookmarkIcon,
			LJD.Image focusedMessageLineTop,
			float pensScale,
			LJD.Brush systemControlBrush,
			float activityBarBoundsPenWidth
		)
		{
			ActivitesCaptionsFont = new LJD.Font(fontName, activitesCaptionsFontSize);
			ActivitesCaptionsBrush = new LJD.Brush(Color.Black);

			ActionCaptionFont = new LJD.Font(fontName, actionCaptionsFontSize);
			RulerMarkFont = new LJD.Font(fontName, rulerMarkFontSize);

			UserIcon = userIcon;
			APIIcon = apiIcon;
			BookmarkIcon = bookmarkIcon;

			SelectedLineBrush = new LJD.Brush(Color.FromArgb(187, 196, 221));
			RulerMarkBrush = new LJD.Brush(Color.Black);
			RulerLinePen = new LJD.Pen(Color.LightGray, 1);

			ProcedureBrush = new LJD.Brush(Color.LightBlue);
			LifetimeBrush = new LJD.Brush(Color.LightGreen);
			NetworkMessageBrush = new LJD.Brush(Color.LightSalmon);
			UnknownActivityBrush = new LJD.Brush(Color.LightGray);
			ActivitiesTopBoundPen = new LJD.Pen(Color.Gray, 1);

			MilestonePen = new LJD.Pen(Color.FromArgb(180, Color.SteelBlue), pensScale * 3f);
			ActivityBarBoundsPen = new LJD.Pen(Color.Gray, activityBarBoundsPenWidth);
			ActivitiesConnectorPen = new LJD.Pen(Color.DarkGray, pensScale * 1f, new[] { 1f, 1f });

			PhaseBrushes = new LJD.Brush[]
			{
				new LJD.Brush(Color.FromArgb(255, 170, 170, 170)),
				new LJD.Brush(Color.FromArgb(255, 0, 150, 136)),
				new LJD.Brush(Color.FromArgb(255, 63, 72, 204)),
				new LJD.Brush(Color.FromArgb(255, 34, 175, 76)),
			};

			UserEventPen = new LJD.Pen(Color.Salmon, pensScale * 2f);
			EventRectBrush = new LJD.Brush(Color.Salmon);
			EventRectPen = new LJD.Pen(Color.Gray, 1);
			EventCaptionBrush = new LJD.Brush(Color.Black);
			EventCaptionFont = ActionCaptionFont;
			EventCaptionStringFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Far);

			BookmarkPen = new LJD.Pen(Color.FromArgb(0x5b, 0x87, 0xe0), pensScale * 1f);

			FocusedMessagePen = new LJD.Pen(Color.Blue, pensScale * 1f);
			FocusedMessageLineTop = focusedMessageLineTop;

			MeasurerPen = new LJD.Pen(Color.DarkGreen, pensScale * 1f, new[] { 4f, 2f });
			MeasurerTextFont = new LJD.Font(fontName, rulerMarkFontSize);
			MeasurerTextBrush = new LJD.Brush(Color.Black);
			MeasurerTextBoxBrush = new LJD.Brush(Color.White);
			MeasurerTextBoxPen = new LJD.Pen(Color.DarkGreen, 1f);
			MeasurerTextFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Center);

			NavigationPanel_InvisibleBackground = new LJD.Brush(Color.FromArgb(235, 235, 235));
			NavigationPanel_VisibleBackground = new LJD.Brush(Color.White);
			SystemControlBrush = systemControlBrush;
			VisibleRangePen = new LJD.Pen(Color.Gray, 1f);

			FoldingSignPen = LJD.Pens.Black;
		}
	};
}
