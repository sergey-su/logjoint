
namespace LogJoint.UI.Timeline
{
    static class StaticMetrics
    {
        /// <summary>
        /// distance between the borders of the control and the bars showing sources
        /// </summary>
        public const int SourcesHorizontalPadding = 4;
        /// <summary>
        /// distance between the bottom border of the control and the bars showing sources
        /// </summary>
        public const int SourcesVerticalPadding = 3;
        /// <summary>
        /// distance between sources' bars (when there are more than one source)
        /// </summary>
        public const int DistanceBetweenSources = 4;
        /// <summary>
        /// The height of the line that is drawn to show the gaps in messages (see DrawCutLine())
        /// </summary>
        public const int CutLineHeight = 2;
        /// <summary>
        /// Minimum height (px) that a time span may have. Time span is a range between time gaps.
        /// We have to limit the minimum size because of usability problems. User must be able to
        /// see and click on any time span even if it very small.
        /// </summary>
        public const int MinimumTimeSpanHeight = 6;

        public const int GapHeight = 5;
        public const int DragAreaHeight = 5;
    };
}
