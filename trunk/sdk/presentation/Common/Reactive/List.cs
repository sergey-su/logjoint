namespace LogJoint.UI.Presenters.Reactive
{
    /// <summary>
    /// A collection of these objects can participate in reactive updates.
    /// If a presenter returns the list as a collection of IListItem,
    /// it's possible for particular platform-specific UI to blindly
    /// follow the changes in the list.
    /// The IListItem objects must be immutable.
    /// </summary>
    public interface IListItem
    {
        /// <summary>
        /// The key is used by UI update procedures to determines if
        /// an item from older version if lost matches that in new version.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Determines if list item is selected. UI blindly obeys the return value.
        /// There is no way the node will be selected in the UI without this property being true
        /// in one of the tree versions.
        /// </summary>
        bool IsSelected { get; }
    };
}