namespace LogJoint
{
    public interface IFiltersManager
    {
        IFiltersList HighlightFilters { get; }
        IFiltersList DisplayFilters { get; }
        FilteringStats FilteringStats { get; }
    };
}
