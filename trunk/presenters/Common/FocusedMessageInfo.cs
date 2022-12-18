using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.UI.Presenters
{
    /// <summary>
    /// Represents the relative position of the focused log message in a list.
    /// The list in question depends on the context. Example is the bookmarks list.
    /// The position is represented as equal range obtained by binary searching the
    /// focused message in the list.
    /// Value of type FocusedMessageInfo can be null when there is no focused message.
    /// </summary>
    public class FocusedMessageInfo
    {
        /// <summary>
        /// The lower bound index.
        /// </summary>
        public int LowerBound { get; internal set; }
        /// <summary>
        /// The upper bound index.
        /// </summary>
        public int UpperBound { get; internal set; }
        /// <summary>
        /// The message for user that explains the time deltas
        /// between the focused message and the neigboring items from the list.
        /// </summary>
        public string Tooltip { get; internal set; }
    }
}
