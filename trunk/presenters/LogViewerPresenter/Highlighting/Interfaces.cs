using LogJoint.Drawing;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
    internal interface IHighlightingManager
    {
        IHighlightingHandler SearchResultHandler { get; }
        IHighlightingHandler HighlightingFiltersHandler { get; }
        IHighlightingHandler SelectionHandler { get; }
        /// <summary>
        /// Generates the text that should be displayed in search results view for a log message.
        /// The returned text can be a line-wise sub-sequence of original message text that skips
        /// lines that don't contain any search matches.
        /// </summary>
        /// <returns>An object with resulting multi-line text and a functions that map between result text lines and the lines of 
        /// original message text</returns>
        MessageDisplayTextInfo GetSearchResultMessageText(IMessage msg, MessageTextGetter originalTextGetter, IFiltersList searchFilters);
    };

    internal interface IHighlightingHandler
    {
        /// <summary>
        /// Enumerates ranges of Message's test that need highlighting. Only the ranges overlapping passed interval as enumerated.
        /// </summary>
        IEnumerable<(int, int, Color)> GetHighlightingRanges(ViewLine vl);
    };
};