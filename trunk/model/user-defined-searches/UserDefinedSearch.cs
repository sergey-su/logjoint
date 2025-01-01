using System;

namespace LogJoint
{
    class UserDefinedSearch : IUserDefinedSearch, IUserDefinedSearchInternal
    {
        IUserDefinedSearchesInternal owner;
        string name;
        IFiltersList filters;

        public UserDefinedSearch(
            IUserDefinedSearchesInternal owner,
            string name,
            IFiltersList filtersList
        )
        {
            this.owner = owner;
            this.name = name;
            this.filters = filtersList;
        }

        string IUserDefinedSearch.Name
        {
            get { return name; }
            set
            {
                if (owner == null)
                    throw new InvalidOperationException();
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(nameof(value));
                if (value == name)
                    return;
                if (owner.ContainsItem(value))
                    throw new NameDuplicateException();
                var oldName = name;
                name = value;
                owner.OnNameChanged(this, oldName);
            }
        }

        IFiltersList IUserDefinedSearch.Filters
        {
            get { return filters; }
            set
            {
                if (owner == null)
                    throw new InvalidOperationException();
                if (value == null)
                    throw new ArgumentNullException();
                if (value == filters)
                    return;
                filters.OnPropertiesChanged -= HandleFiltersListChange;
                filters.OnFiltersListChanged -= HandleFiltersListChange;
                filters = value;
                filters.OnPropertiesChanged += HandleFiltersListChange;
                filters.OnFiltersListChanged += HandleFiltersListChange;
                owner.OnFiltersChanged(this);
            }
        }

        void IUserDefinedSearchInternal.DetachFromOwner(IUserDefinedSearchesInternal expectedOwner)
        {
            if (owner == null)
                throw new InvalidOperationException();
            if (owner != expectedOwner)
                throw new ArgumentException(nameof(expectedOwner));
            owner = null;
        }

        void HandleFiltersListChange(object sender, EventArgs args)
        {
            owner?.OnFiltersChanged(this);
        }
    };
}
