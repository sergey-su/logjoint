using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using LogJoint.Search;

namespace LogJoint
{
    internal class Filter : IDisposable, IFilter
    {
        public Filter(FilterAction action, string initialName, bool enabled,
            Search.Options options, FilterTimeRange timeRange, IFiltersFactory factory, RegularExpressions.IRegexFactory regexFactory)
        {
            this.factory = factory;
            this.regexFactory = regexFactory;
            this.initialName = initialName ?? throw new ArgumentNullException(nameof(initialName));
            this.enabled = enabled;
            this.action = action;
            this.timeRange = timeRange;

            this.options = options;

            // Filters ignores following flags passed.
            // Actually used values are provided later when filters are applied.
            this.options.ReverseSearch = false;

            InvalidateName();
        }

        public Filter(XElement e, IFiltersFactory factory, RegularExpressions.IRegexFactory regexFactory)
        {
            this.factory = factory;
            this.regexFactory = regexFactory;

            LoadInternal(e);
        }

        IFiltersFactory IFilter.Factory { get { return factory; } }

        FilterAction IFilter.Action
        {
            get
            {
                CheckDisposed();
                return action;
            }
            set
            {
                CheckDisposed();
                if (action == value)
                    return;
                action = value;
                OnChange(true, false);
                InvalidateDefaultAction();
            }
        }

        string IFilter.Name
        {
            get
            {
                CheckDisposed();
                InternalEnsureName();
                return name;
            }
        }

        string IFilter.InitialName { get { return initialName; } }

        string IFilter.UserDefinedName
        {
            get { return userDefinedName; }
            set
            {
                CheckDisposed();
                if (string.IsNullOrEmpty(value))
                    value = null;
                userDefinedName = value;
                InvalidateName();
                OnChange(false, false);
            }
        }

        bool IFilter.Enabled
        {
            get
            {
                CheckDisposed();
                return enabled;
            }
            set
            {
                CheckDisposed();
                if (enabled == value)
                    return;
                enabled = value;
                OnChange(true, false);
                InvalidateDefaultAction();
            }
        }

        Search.Options IFilter.Options
        {
            get
            {
                CheckDisposed();
                return options;
            }
            set
            {
                CheckDisposed();

                this.options = value;

                InvalidateName();

                OnChange(true, true);
            }
        }

        FilterTimeRange IFilter.TimeRange
        {
            get
            {
                CheckDisposed();
                return timeRange;
            }
            set
            {
                CheckDisposed();

                this.timeRange = value;

                OnChange(true, true);
            }
        }


        IFiltersList IFilter.Owner { get { return owner; } }

        IFilter IFilter.Clone()
        {
            IFilter ret = factory.CreateFilter(action, initialName, enabled, options, timeRange);
            ret.UserDefinedName = userDefinedName;
            return ret;
        }

        bool IFilter.IsDisposed
        {
            get { return isDisposed; }
        }

        IFilterBulkProcessing IFilter.StartBulkProcessing(
            MessageTextGetter messageTextGetter, bool reverseMatchDirection, bool timeboxedMatching)
        {
            CheckDisposed();
            var tmp = options;
            tmp.MessageTextGetter = messageTextGetter;
            tmp.ReverseSearch = reverseMatchDirection;
            MessageTimestamp? toRangeBound(DateTime? t) =>
                t != null ? new MessageTimestamp(t.Value) : null;
            return new BulkProcessing()
            {
                searchState = tmp.BeginSearch(regexFactory, timeboxedMatching),
                timeRangeBegin = toRangeBound(timeRange?.Begin),
                timeRangeEnd = toRangeBound(timeRange?.End),
            };
        }

        void IFilter.Save(XElement e)
        {
            SaveInternal(e);
        }

        void IFilter.SetOwner(IFiltersList newOwner)
        {
            CheckDisposed();
            if (newOwner != null && owner != null)
                throw new InvalidOperationException("Filter can not be attached to FiltersList: already attached to another list");
            owner = newOwner;
        }

        void IDisposable.Dispose()
        {
            if (isDisposed)
                return;
            owner = null;
            isDisposed = true;
        }


        #region Implementation

        void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(this.ToString());
        }

        void InvalidateName()
        {
            this.name = null;
        }

        void InvalidateDefaultAction()
        {
            if (owner != null)
                owner.InvalidateDefaultAction();
        }

        void InternalUpdateName()
        {
            if (userDefinedName != null)
            {
                name = userDefinedName;
                return;
            }
            List<string> templateIndependentModifiers = new List<string>();
            GetTemplateIndependentModifiers(templateIndependentModifiers);
            if (!string.IsNullOrEmpty(options.Template))
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(options.Template);
                List<string> modifiers = new List<string>();
                GetTemplateDependentModifiers(modifiers);
                modifiers.AddRange(templateIndependentModifiers);
                ConcatModifiers(builder, modifiers);
                name = builder.ToString();
            }
            else if (templateIndependentModifiers.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("<any text>");
                ConcatModifiers(builder, templateIndependentModifiers);
                name = builder.ToString();
            }
            else
            {
                name = initialName;
            }
        }

        static void ConcatModifiers(StringBuilder ret, List<string> modifiers)
        {
            if (modifiers.Count > 0)
            {
                ret.Append(" (");
                for (int i = 0; i < modifiers.Count; ++i)
                {
                    if (i > 0)
                        ret.Append(", ");
                    ret.Append(modifiers[i]);
                }
                ret.Append(")");
            }
        }

        void GetTemplateDependentModifiers(List<string> modifiers)
        {
            if (options.MatchCase)
                modifiers.Add("match case");
            if (options.WholeWord)
                modifiers.Add("whole word");
            if (options.Regexp)
                modifiers.Add("regexp");
        }

        void GetTemplateIndependentModifiers(List<string> modifiers)
        {
            var contentTypes = options.ContentTypes & MessageFlag.ContentTypeMask;
            if (contentTypes != MessageFlag.ContentTypeMask)
            {
                if ((contentTypes & MessageFlag.Info) != 0)
                    modifiers.Add("infos");
                if ((contentTypes & MessageFlag.Warning) != 0)
                    modifiers.Add("warns");
                if ((contentTypes & MessageFlag.Error) != 0)
                    modifiers.Add("errs");
            }
        }

        void InternalEnsureName()
        {
            CheckDisposed();
            if (name != null)
                return;
            InternalUpdateName();
        }

        protected void OnChange(bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
        {
            if (owner != null)
                owner.FireOnPropertiesChanged(this, changeAffectsFilterResult, changeAffectsPreprocessingResult);
        }

        void SaveInternal(XElement e)
        {
            options.Save(e);
            if (!enabled)
                e.SetAttributeValue("enabled", "0");
            e.SetAttributeValue("action", (int)action);
            if (initialName != "")
                e.SetAttributeValue("initial-name", initialName);
            if (userDefinedName != null)
                e.SetAttributeValue("given-name", userDefinedName);
            if (timeRange?.Begin != null)
                e.SetAttributeValue("time-begin", new MessageTimestamp(timeRange.Begin.Value).StoreToLoselessFormat());
            if (timeRange?.End != null)
                e.SetAttributeValue("time-end", new MessageTimestamp(timeRange.End.Value).StoreToLoselessFormat());
        }

        void LoadInternal(XElement e)
        {
            options = new Search.Options().Load(e);
            enabled = e.SafeIntValue("enabled", 1) != 0;
            action = (FilterAction)e.SafeIntValue("action", (int)FilterAction.Include);
            initialName = e.AttributeValue("initial-name", defaultValue: "");
            userDefinedName = e.AttributeValue("given-name", defaultValue: null);

            DateTime? parseRangeTime(string str)
                => string.IsNullOrEmpty(str) ? null : MessageTimestamp.ParseFromLoselessFormat(str).ToUnspecifiedTime();
            DateTime? timeRangeBegin = parseRangeTime(e.AttributeValue("time-begin"));
            DateTime? timeRangeEnd = parseRangeTime(e.AttributeValue("time-end"));
            if (timeRangeBegin != null || timeRangeEnd != null)
                timeRange = new FilterTimeRange(timeRangeBegin, timeRangeEnd);
        }

        #endregion

        #region Members

        private readonly IFiltersFactory factory;
        private readonly RegularExpressions.IRegexFactory regexFactory;

        private bool isDisposed;
        private IFiltersList owner;
        private string initialName;
        private string userDefinedName;

        private FilterAction action;
        private bool enabled;
        private Search.Options options;
        private FilterTimeRange timeRange;

        private string name;

        #endregion

        class BulkProcessing : IFilterBulkProcessing
        {
            internal Search.SearchState searchState;
            internal MessageTimestamp? timeRangeBegin;
            internal MessageTimestamp? timeRangeEnd;

            void IDisposable.Dispose()
            {
            }

            Search.MatchedTextRange? IFilterBulkProcessing.Match(IMessage message, int? startFromChar)
            {
                if (timeRangeBegin != null && message.Time < timeRangeBegin.Value)
                    return null;
                if (timeRangeEnd != null && message.Time > timeRangeEnd.Value)
                    return null;
                return searchState.SearchInMessageText(message, startFromChar);
            }

            Search.MatchedTextRange? IFilterBulkProcessing.Match(StringSlice text, int? startFromChar)
            {
                return searchState.SearchInText(text, startFromChar);
            }
        };
    };
}
