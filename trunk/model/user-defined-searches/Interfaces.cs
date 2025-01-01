using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint
{
    public interface IUserDefinedSearches
    {
        IEnumerable<IUserDefinedSearch> Items { get; }
        bool ContainsItem(string name);
        IUserDefinedSearch AddNew();
        void Delete(IUserDefinedSearch search);
        void Export(IUserDefinedSearch[] searches, Stream stm);
        Task Import(Stream stm, Func<string, Task<NameDuplicateResolution>> dupesResolver);

        event EventHandler OnChanged;
    };

    /// <summary>
    /// Defines search parameters that combines multiple search rules under one name.
    /// The concept is called "Filter" in UI.
    /// </summary>
    public interface IUserDefinedSearch
    {
        string Name { get; set; }
        IFiltersList Filters { get; set; }
    };

    public class NameDuplicateException : Exception
    {
    };

    public enum NameDuplicateResolution
    {
        Skip,
        Overwrite,
        Cancel
    };

    internal interface IUserDefinedSearchesInternal : IUserDefinedSearches
    {
        void OnNameChanged(IUserDefinedSearch sender, string oldName);
        void OnFiltersChanged(IUserDefinedSearch sender);
    };

    internal interface IUserDefinedSearchInternal
    {
        void DetachFromOwner(IUserDefinedSearchesInternal expectedOwner);
    };
}
