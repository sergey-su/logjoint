using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static LogJoint.Workspaces.WorkspaceDTO;

namespace LogJoint.UI.Presenters.FilterDialog
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly IChangeNotification changeNotification;
        readonly ILogSourcesManager logSources;
        readonly IColorTable highlightColorsTable;
        IView view;
        List<(FilterAction action, string name, Color? color)> actionsOptions;
        bool scopeSupported;
        ImmutableList<ScopeNode> scopeItems;
        ImmutableList<MessageTypeItem> messageTypeItems = ImmutableList<MessageTypeItem>.Empty;
        CheckBoxId checkedBoxes;
        string userDefinedName;
        IFilter currentFilter;
        TaskCompletionSource<bool> currentTask;
        DialogConfig dialogConfig = new DialogConfig()
        {
            Title = "",
            ActionComboBoxOptions = Array.Empty<KeyValuePair<string, Color?>>(),
        };
        string template;
        int actionComboBoxValue = -1;
        static readonly (MessageFlag flags, string name)[] typeFlagsList =
        {
            (MessageFlag.Error, "Errors"),
            (MessageFlag.Warning, "Warnings"),
            (MessageFlag.Info, "Infos")
        };
        const string changeLinkText = "change";
        const string resetLinkText = "auto";
        readonly Func<NameEditBoxProperties> nameEditBoxProperties;
        readonly Func<FilterData> outputFilterData;

        public Presenter(
            IChangeNotification changeNotification,
            ILogSourcesManager logSources,
            IColorTable highlightColorsTable
        )
        {
            this.changeNotification = changeNotification;
            this.logSources = logSources;
            this.highlightColorsTable = highlightColorsTable;
            this.outputFilterData = Selectors.Create(() => (
                userDefinedName, actionComboBoxValue, checkedBoxes, template,
                scopeSupported, scopeItems, messageTypeItems), data => new FilterData()
                {
                    template = data.template,
                    actionComboBoxValue = data.actionComboBoxValue,
                    checkedBoxes = data.checkedBoxes,
                    messageTypeItems = data.messageTypeItems,
                    scopeSupported = data.scopeSupported,
                    scopeItems = data.scopeItems,
                    userDefinedName = data.userDefinedName
                });
            this.nameEditBoxProperties = Selectors.Create(() => userDefinedName, outputFilterData, (udn, filterData) =>
            {
                var userDefinedNameSet = userDefinedName != null;
                string AutomaticName()
                {
                    if (currentFilter == null)
                        return "";
                    using var tempFilter = currentFilter.Clone();
                    SetFilterData(tempFilter, filterData);
                    return tempFilter.Name;
                }
                return new NameEditBoxProperties()
                {
                    Value = userDefinedName ?? AutomaticName(),
                    Enabled = userDefinedNameSet,
                    LinkText = userDefinedNameSet ? resetLinkText : changeLinkText
                };
            });
        }

        Task<bool> IPresenter.ShowTheDialog(IFilter forFilter, FiltersListPurpose filtersListPurpose)
        {
            Reset();

            currentFilter = forFilter;

            currentTask = new TaskCompletionSource<bool>();
            actionsOptions = MakeActionsOptions(filtersListPurpose, highlightColorsTable);
            scopeSupported = filtersListPurpose == FiltersListPurpose.Highlighting;

            scopeItems = CreateScopeItems(currentFilter.Options.Scope);
            messageTypeItems = CreateMessageTypeItems(currentFilter.Options.ContentTypes);

            static CheckBoxId cbFlag(CheckBoxId id, bool val) => val ? id : CheckBoxId.None;
            checkedBoxes =
                cbFlag(CheckBoxId.FilterEnabled, currentFilter.Enabled) |
                cbFlag(CheckBoxId.MatchCase, currentFilter.Options.MatchCase) |
                cbFlag(CheckBoxId.RegExp, currentFilter.Options.Regexp) |
                cbFlag(CheckBoxId.WholeWord, currentFilter.Options.WholeWord);

            userDefinedName = currentFilter.UserDefinedName;

            template = currentFilter.Options.Template;

            actionComboBoxValue = actionsOptions.IndexOf(i => i.action == currentFilter.Action).GetValueOrDefault(-1);

            dialogConfig = new DialogConfig()
            {
                Title = "Filter rule",
                ActionComboBoxOptions = actionsOptions.Select(a => new KeyValuePair<string, Color?>(a.name, a.color)).ToArray(),
            };
            changeNotification.Post();
            return currentTask.Task;
        }

        IChangeNotification IViewModel.ChangeNotification { get { return changeNotification; } }

        bool IViewModel.IsVisible => currentTask != null;

        DialogConfig IViewModel.Config => dialogConfig;

        IReadOnlyList<IScopeItem> IViewModel.ScopeItems => scopeItems;

        void IViewModel.OnScopeItemCheck(IScopeItem item, bool checkedValue)
        {
            if (scopeSupported)
                ((ScopeNode)item).HandleClick(checkedValue);
        }

        void IViewModel.OnScopeItemSelect(IScopeItem item)
        {
            if (scopeSupported && item is ScopeNode node && !node.IsSelected)
            {
                scopeItems = ImmutableList.CreateRange(
                    scopeItems.Select(i => (i.Index == node.Index) == i.IsSelected ? i : i.SetSelected(i.Index == node.Index)));
                changeNotification.Post();
            }
        }

        IReadOnlyList<IMessageTypeItem> IViewModel.MessageTypeItems => messageTypeItems;

        void IViewModel.OnMessageTypeItemCheck(IMessageTypeItem item, bool checkedValue)
        {
            if (item is MessageTypeItem impl && impl.IsChecked != checkedValue)
            {
                messageTypeItems = messageTypeItems.SetItem(impl.Index,
                    new MessageTypeItem(impl.ToString(), impl.Index, checkedValue, impl.IsSelected));
                changeNotification.Post();
            }
        }

        void IViewModel.OnMessageTypeItemSelect(IMessageTypeItem item)
        {
            if (item is MessageTypeItem itemToSelect && !itemToSelect.IsSelected)
            {
                messageTypeItems = ImmutableList.CreateRange(messageTypeItems.Select(i =>
                    i.IsSelected == (i.Index == itemToSelect.Index) ? i :
                    new MessageTypeItem(i.ToString(), i.Index, i.IsChecked, i.Index == itemToSelect.Index)));
                changeNotification.Post();
            }
        }


        CheckBoxId IViewModel.CheckedBoxes => checkedBoxes;

        NameEditBoxProperties IViewModel.NameEdit => nameEditBoxProperties();

        string IViewModel.Template => template;

        void IViewModel.OnNameEditLinkClicked()
        {
            if (userDefinedName != null)
            {
                userDefinedName = null;
                changeNotification.Post();
            }
            else
            {
                userDefinedName = nameEditBoxProperties().Value;
                changeNotification.Post();
                view?.PutFocusOnNameEdit();
            }
        }

        void IViewModel.OnCancelled() => Reset();

        void IViewModel.OnConfirmed()
        {
            if (currentTask != null)
            {
                SetFilterData(currentFilter, outputFilterData());
                currentTask.TrySetResult(true);
                currentTask = null;
                changeNotification.Post();
            }
        }

        void IViewModel.OnCheckBoxCheck(CheckBoxId cb, bool checkedValue)
        {
            CheckBoxId newCheckedBoxes = checkedValue ? checkedBoxes | cb : checkedBoxes & ~cb;
            if (newCheckedBoxes != checkedBoxes)
            {
                checkedBoxes = newCheckedBoxes;
                changeNotification.Post();
            }
        }

        void IViewModel.OnNameChange(string value)
        {
            if (userDefinedName != null && userDefinedName != value)
            {
                userDefinedName = value;
                changeNotification.Post();
            }
        }

        void IViewModel.OnTemplateChange(string value)
        {
            if (template != value)
            {
                template = value;
                changeNotification.Post();
            }
        }


        int IViewModel.ActionComboBoxValue => actionComboBoxValue;

        void IViewModel.OnActionComboBoxValueChange(int value)
        {
            if (actionComboBoxValue != value)
            {
                actionComboBoxValue = value;
                changeNotification.Post();
            }
        }

        void IViewModel.SetView(IView view)
        {
            this.view = view;
        }

        void Reset()
        {
            if (currentTask != null)
            {
                currentTask.TrySetResult(false);
                currentTask = null;
                changeNotification.Post();
            }
        }

        ImmutableList<ScopeNode> CreateScopeItems(IFilterScope target)
        {
            if (!scopeSupported)
                return null;

            var items = ImmutableList.CreateBuilder<ScopeNode>();

            bool matchesAllSources = target.ContainsEverything;
            items.Add(new AllSources(items.Count, matchesAllSources, false, this));

            foreach (ILogSource s in logSources?.Items ?? Array.Empty<ILogSource>())
            {
                bool matchesSource = matchesAllSources || target.ContainsEverythingFromSource(s);
                items.Add(new SourceNode(items.Count, s, matchesSource, false, this));

                foreach (IThread t in s.Threads.Items)
                {
                    bool matchesThread = matchesSource || target.ContainsEverythingFromThread(t);
                    items.Add(new ThreadNode(items.Count, t, matchesThread, false, this));
                }
            }

            return items.ToImmutable();
        }

        ImmutableList<MessageTypeItem> CreateMessageTypeItems(MessageFlag contentTypes)
        {
            return ImmutableList.CreateRange(typeFlagsList.Select(
                (item, i) => new MessageTypeItem(item.name, i, (contentTypes & item.flags) == item.flags, false)));
        }

        void SetScopeItemChecked(int i, bool isChecked)
        {
            if (scopeItems[i].IsChecked == isChecked)
                return;
            scopeItems = scopeItems.SetItem(i, scopeItems[i].SetChecked(isChecked));
            changeNotification.Post();
        }

        class MessageTypeItem : IMessageTypeItem
        {
            readonly string name;
            readonly int index;
            readonly bool isChecked;
            readonly bool isSelected;

            public MessageTypeItem(string name, int index, bool isChecked, bool isSelected)
            {
                this.name = name;
                this.isChecked = isChecked;
                this.index = index;
                this.isSelected = isSelected;
            }

            public int Index => index;
            public bool IsChecked => isChecked;
            public bool IsSelected => isSelected;
            public override string ToString() => name;
            bool IMessageTypeItem.IsChecked => isChecked;
            string IListItem.Key => name;
            bool IListItem.IsSelected => isSelected;
        }

        abstract class ScopeNode : IScopeItem
        {
            protected readonly int indent;
            protected readonly string key;
            protected readonly bool isChecked;
            protected readonly bool isSelected;

            protected ScopeNode(int idx, Presenter owner, int indent, string key, bool isChecked, bool isSelected)
            {
                this.Index = idx;
                this.Owner = owner;
                this.indent = indent;
                this.key = key;
                this.isChecked = isChecked;
                this.isSelected = isSelected;
            }

            public abstract ScopeNode SetChecked(bool isChecked);
            public abstract ScopeNode SetSelected(bool isSelected);

            public abstract void HandleClick(bool checkedValue);
            public readonly int Index;
            public readonly Presenter Owner;
            public bool IsChecked => isChecked;
            public bool IsSelected => isSelected;

            int IScopeItem.Indent => indent;
            string IListItem.Key => key;
            bool IScopeItem.IsChecked => isChecked;
            bool IListItem.IsSelected => isSelected;
        }

        class AllSources : ScopeNode
        {
            public AllSources(int idx, bool isChecked, bool isSelected, Presenter owner) :
                base(idx, owner, 0, "<<all>>", isChecked, isSelected)
            {
            }

            public override ScopeNode SetChecked(bool isChecked) => new AllSources(Index, isChecked, isSelected, Owner);

            public override ScopeNode SetSelected(bool isSelected) => new AllSources(Index, isChecked, isSelected, Owner);

            public override string ToString()
            {
                return "All threads from all sources";
            }

            public override void HandleClick(bool checkedValue)
            {
                for (int i = 0; i < Owner.scopeItems.Count; ++i)
                    Owner.SetScopeItemChecked(i, checkedValue);
            }
        };

        class SourceNode : ScopeNode
        {
            public readonly ILogSource Source;

            public SourceNode(int idx, ILogSource src, bool isChecked, bool isSelected, Presenter owner) :
                base(idx, owner, 1, src.ConnectionId, isChecked, isSelected)
            {
                this.Source = src;
            }

            public override ScopeNode SetChecked(bool isChecked) => new SourceNode(Index, Source, isChecked, isSelected, Owner);

            public override ScopeNode SetSelected(bool isSelected) => new SourceNode(Index, Source, isChecked, isSelected, Owner);

            public override string ToString()
            {
                return "All threads from " + Source.DisplayName;
            }

            public override void HandleClick(bool checkedValue)
            {
                for (int i = 0; i < Owner.scopeItems.Count; ++i)
                {
                    var item = Owner.scopeItems[i];
                    if (object.ReferenceEquals(item, this))
                    {
                        Owner.SetScopeItemChecked(i, checkedValue);
                    }
                    else if (item is AllSources)
                    {
                        if (!checkedValue)
                            Owner.SetScopeItemChecked(i, false);
                    }
                    else if (item is ThreadNode threadNode)
                    {
                        if (threadNode.Thread.LogSource == Source)
                            Owner.SetScopeItemChecked(i, checkedValue);
                    }
                }
            }
        };

        class ThreadNode : ScopeNode
        {
            public readonly IThread Thread;

            public ThreadNode(int idx, IThread t, bool isChecked, bool isSelected, Presenter owner)
                : base(idx, owner, 2, t.ID, isChecked, isSelected)
            {
                this.Thread = t;
            }

            public override ScopeNode SetChecked(bool isChecked) => new ThreadNode(Index, Thread, isChecked, isSelected, Owner);

            public override ScopeNode SetSelected(bool isSelected) => new ThreadNode(Index, Thread, isChecked, isSelected, Owner);

            public override string ToString()
            {
                return Thread.DisplayName;
            }

            public override void HandleClick(bool checkedValue)
            {
                for (int i = 0; i < Owner.scopeItems.Count; ++i)
                {
                    object item = Owner.scopeItems[i];
                    if (ReferenceEquals(item, this))
                    {
                        Owner.SetScopeItemChecked(i, checkedValue);
                    }
                    else if (item is AllSources)
                    {
                        if (!checkedValue)
                            Owner.SetScopeItemChecked(i, false);
                    }
                    else if (item is SourceNode sourceNode)
                    {
                        if (!checkedValue && sourceNode.Source == Thread.LogSource)
                            Owner.SetScopeItemChecked(i, false);
                    }
                }
            }
        };

        class FilterData
        {
            public string userDefinedName;
            public int actionComboBoxValue;
            public CheckBoxId checkedBoxes;
            public string template;
            public bool scopeSupported;
            public ImmutableList<ScopeNode> scopeItems;
            public ImmutableList<MessageTypeItem> messageTypeItems;
        };

        void SetFilterData(IFilter destinationFilter, FilterData data)
        {
            destinationFilter.UserDefinedName = data.userDefinedName;
            destinationFilter.Action = actionsOptions.ElementAtOrDefault(data.actionComboBoxValue).action;
            destinationFilter.Enabled = (data.checkedBoxes & CheckBoxId.FilterEnabled) != 0;
            destinationFilter.Options = new Search.Options()
            {
                Template = data.template,
                MatchCase = (data.checkedBoxes & CheckBoxId.MatchCase) != 0,
                Regexp = (data.checkedBoxes & CheckBoxId.RegExp) != 0,
                WholeWord = (data.checkedBoxes & CheckBoxId.WholeWord) != 0,
                Scope = CreateScope(data.scopeSupported, data.scopeItems, destinationFilter.Factory),
                ContentTypes = MakeContextTypesMask(data.messageTypeItems)
            };
        }

        static IFilterScope CreateScope(bool scopeSupported, IReadOnlyList<ScopeNode> items, IFiltersFactory filtersFactory)
        {
            if (!scopeSupported)
                return filtersFactory.CreateScope();

            List<ILogSource> sources = new List<ILogSource>();
            List<IThread> threads = new List<IThread>();

            for (int i = 0; i < items.Count;)
            {
                ScopeNode item = items[i];
                bool isChecked = item.IsChecked;

                if (item is AllSources)
                {
                    if (isChecked)
                    {
                        return filtersFactory.CreateScope();
                    }
                    ++i;
                    continue;
                }

                if (item is SourceNode sourceNode)
                {
                    if (isChecked)
                    {
                        sources.Add(sourceNode.Source);
                        ++i;
                        while (i < items.Count && items[i] is ThreadNode)
                            ++i;
                    }
                    else
                    {
                        ++i;
                    }
                    continue;
                }

                if (item is ThreadNode threadNode)
                {
                    if (isChecked)
                    {
                        threads.Add(threadNode.Thread);
                    }
                    ++i;
                    continue;
                }

                throw new InvalidOperationException("Unknown node type");
            }

            return filtersFactory.CreateScope(sources, threads);
        }

        static MessageFlag MakeContextTypesMask(IReadOnlyList<IMessageTypeItem> messageTypeItems)
        {
            var f = MessageFlag.None;
            for (int i = 0; i < Math.Min(typeFlagsList.Length, messageTypeItems.Count); ++i)
            {
                if (messageTypeItems[i].IsChecked)
                    f |= typeFlagsList[i].flags;
            }
            return f;
        }

        static List<(FilterAction, string, Color?)> MakeActionsOptions(FiltersListPurpose purpose, IColorTable highlightColorsTable)
        {
            var actionOptions = new List<(FilterAction, string, Color?)>();

            string excludeDescription;
            if (purpose == FiltersListPurpose.Highlighting)
                excludeDescription = "Exclude from highlighting";
            else if (purpose == FiltersListPurpose.Search)
                excludeDescription = "Exclude from search results";
            else
                excludeDescription = "Exclude";
            actionOptions.Add((FilterAction.Exclude, excludeDescription, new Color?()));

            if (purpose == FiltersListPurpose.Search)
            {
                actionOptions.Add((FilterAction.Include, "Include to search result", new Color?()));
            }

            string includeAndColorizeFormat;
            if (purpose == FiltersListPurpose.Highlighting)
                includeAndColorizeFormat = " Highlight with color #{0} ";
            else if (purpose == FiltersListPurpose.Search)
                includeAndColorizeFormat = " Include to search result and highlight with color #{0} ";
            else
                includeAndColorizeFormat = " Include and highlight with color #{0} ";

            for (var a = FilterAction.IncludeAndColorizeFirst; a <= FilterAction.IncludeAndColorizeLast; ++a)
            {
                actionOptions.Add((a,
                    string.Format(includeAndColorizeFormat, a - FilterAction.IncludeAndColorizeFirst + 1),
                    a.ToColor(highlightColorsTable.Items)));
            }

            return actionOptions;
        }
    };
};