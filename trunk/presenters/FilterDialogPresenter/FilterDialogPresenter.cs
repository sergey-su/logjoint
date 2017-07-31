using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.FilterDialog
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly ILogSourcesManager logSources;
		readonly List<Tuple<FilterAction, string, ModelColor?>> actionsOptions;
		List<ScopeItem> scopeItems;
		bool clickLock;
		bool userDefinedNameSet;
		IFilter currentFilter;
		static readonly MessageFlag[] typeFlagsList = 
		{
			MessageFlag.Error,
			MessageFlag.Warning,
			MessageFlag.Info
		};
		const string changeLinkText = "change";
		const string resetLinkText = "auto";

		public Presenter(
			ILogSourcesManager logSources, 
			IFiltersList filtersList, 
			IView view
		)
		{
			this.logSources = logSources;
			this.view = view;
			this.actionsOptions = MakeActionsOptions(filtersList.Purpose);
			view.SetEventsHandler(this);
		}

		bool IPresenter.ShowTheDialog(IFilter forFilter)
		{
			currentFilter = forFilter;
			WriteView(forFilter);
			if (!view.ShowDialog())
				return false;
			ReadView(forFilter);
			return true;
		}

		void IViewEvents.OnScopeItemChecked(ScopeItem item, bool checkedValue)
		{
			if (clickLock)
				return;
			clickLock = true;
			((Node)item).Click(checkedValue);
			clickLock = false;
		}

		void IViewEvents.OnCriteriaInputChanged()
		{
			if (clickLock)
				return;
			RefreshAutomaticNameTextBox();
		}

		void IViewEvents.OnNameEditLinkClicked()
		{
			if (userDefinedNameSet)
			{
				userDefinedNameSet = false;
				RefreshAutomaticNameTextBox();
			}
			else
			{
				userDefinedNameSet = true;
				view.SetNameEditProperties(new NameEditBoxProperties()
				{
					Value = view.GetData().NameEditBoxProperties.Value, 
					Enabled = true,
					LinkText = resetLinkText,
				});
				view.PutFocusOnNameEdit();
			}
		}

		void RefreshAutomaticNameTextBox()
		{
			if (userDefinedNameSet)
				return;
			using (var tempFilter = currentFilter.Clone())
			{
				ReadView(tempFilter);
				view.SetNameEditProperties(new NameEditBoxProperties()
				{
					Value = tempFilter.Name, 
					Enabled = false,
					LinkText = changeLinkText,
				});
			}
		}

		List<KeyValuePair<ScopeItem, bool>> CreateScopeItems(IFilterScope target)
		{
			var items = new List<KeyValuePair<ScopeItem, bool>>();

			Action<ScopeItem, bool> add = (i, isChecked) => items.Add(new KeyValuePair<ScopeItem, bool>(i, isChecked));

			bool matchesAllSources = target.ContainsEverything;
			add(new AllSources(items.Count, this), matchesAllSources);

			foreach (ILogSource s in logSources?.Items ?? new ILogSource[0])
			{
				bool matchesSource = matchesAllSources || target.ContainsEverythingFromSource(s);
				add(new SourceNode(items.Count, s, this), matchesSource);

				foreach (IThread t in s.Threads.Items)
				{
					bool matchesThread = matchesSource || target.ContainsEverythingFromThread(t);
					add(new ThreadNode(items.Count, t, this), matchesThread);
				}
			}

			return items;
		}

		IEnumerable<bool> CreateTypes(IFilter filter)
		{
			var types = filter.Options.ContentTypes;
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				yield return (typeFlagsList[i] & types) == typeFlagsList[i];
			}
		}

		abstract class Node: ScopeItem
		{
			protected Node(int idx, Presenter owner)
			{
				this.Index = idx;
				this.Owner = owner;
			}
			public abstract void Click(bool checkedValue);
			public readonly int Index;
			public readonly Presenter Owner;
		};

		class AllSources : Node
		{
			public AllSources(int idx, Presenter owner) : base(idx, owner)
			{
			}

			public override string ToString()
			{
				return "All threads from all sources";
			}

			public override void Click(bool checkedValue)
			{
				bool f = !checkedValue;
				for (int i = 0; i < Owner.scopeItems.Count; ++i)
					Owner.view.SetScopeItemChecked(i, f);
			}
		};

		class SourceNode : Node
		{
			public readonly ILogSource Source;

			public SourceNode(int idx, ILogSource src, Presenter owner) : base(idx, owner)
			{
				this.Source = src;
				this.Indent = 1;
			}

			public override string ToString()
			{
				return "All threads from " + Source.DisplayName;
			}

			public override void Click(bool checkedValue)
			{
				bool f = !checkedValue;
				for (int i = 0; i < Owner.scopeItems.Count; ++i)
				{
					var item = Owner.scopeItems[i];
					if (object.ReferenceEquals(item, this))
					{
						Owner.view.SetScopeItemChecked(i, f);
					}
					else if (item is AllSources)
					{
						if (!f)
							Owner.view.SetScopeItemChecked(i, false);
					}
					else if (item is ThreadNode)
					{
						if (((ThreadNode)item).Thread.LogSource == Source)
							Owner.view.SetScopeItemChecked(i, f);
					}
				}
			}
		};

		class ThreadNode : Node
		{
			public readonly IThread Thread;

			public ThreadNode(int idx, IThread t, Presenter owner)
				: base(idx, owner)
			{
				this.Thread = t;
				this.Indent = 2;
			}

			public override string ToString()
			{
				return Thread.DisplayName;
			}

			public override void Click(bool checkedValue)
			{
				bool f = !checkedValue;
				for (int i = 0; i < Owner.scopeItems.Count; ++i)
				{
					object item = Owner.scopeItems[i];
					if (object.ReferenceEquals(item, this))
					{
						Owner.view.SetScopeItemChecked(i, f);
					}
					else if (item is AllSources)
					{
						if (!f)
							Owner.view.SetScopeItemChecked(i, false);
					}
					else if (item is SourceNode)
					{
						if (!f && ((SourceNode)item).Source == Thread.LogSource)
							Owner.view.SetScopeItemChecked(i, false);
					}
				}
			}
		};

		void WriteView(IFilter filter)
		{
			var tempScopeItems = CreateScopeItems(filter.Options.Scope);
			this.scopeItems = tempScopeItems.Select(i => i.Key).ToList();
			this.userDefinedNameSet = filter.UserDefinedName != null;
			clickLock = true;

			view.SetData(
				"Filter rule",
				actionsOptions.Select(a => new KeyValuePair<string, ModelColor?>(a.Item2, a.Item3)).ToArray(),
				new[]
				{
						"Errors",
						"Warnings",
						"Infos"
				},
				new DialogValues()
				{
					NameEditBoxProperties = new NameEditBoxProperties()
					{
						Value = filter.Name,
						Enabled = userDefinedNameSet,
						LinkText = userDefinedNameSet ? resetLinkText : changeLinkText
					},
					EnabledCheckboxValue = filter.Enabled,
					TemplateEditValue = filter.Options.Template,
					MatchCaseCheckboxValue = filter.Options.MatchCase,
					RegExpCheckBoxValue = filter.Options.Regexp,
					WholeWordCheckboxValue = filter.Options.WholeWord,
					ActionComboBoxValue = GetActionComboBoxValue(filter.Action),
					ScopeItems = tempScopeItems,
					TypesCheckboxesValues = CreateTypes(filter).ToList()
				}
			);
			clickLock = false;
		}

		void ReadView(IFilter filter)
		{
			var data = view.GetData();

			filter.UserDefinedName = userDefinedNameSet ? data.NameEditBoxProperties.Value : null;
			filter.Action = GetFilterAction(data.ActionComboBoxValue);
			filter.Enabled = data.EnabledCheckboxValue;
			filter.Options = new Search.Options()
			{
				Template = data.TemplateEditValue,
				MatchCase = data.MatchCaseCheckboxValue,
				Regexp = data.RegExpCheckBoxValue,
				WholeWord = data.WholeWordCheckboxValue,
				Scope = CreateScope(data.ScopeItems, filter.Factory),
				ContentTypes = GetTypes(data.TypesCheckboxesValues)
			};
		}

		int GetActionComboBoxValue(FilterAction a)
		{
			return actionsOptions.IndexOf(i => i.Item1 == a).GetValueOrDefault(-1);
		}

		FilterAction GetFilterAction(int actionComboBoxValue)
		{
			return (actionsOptions.ElementAtOrDefault(actionComboBoxValue)?.Item1)
				.GetValueOrDefault(FilterAction.Exclude);
		}

		IFilterScope CreateScope(List<KeyValuePair<ScopeItem, bool>> items, IFiltersFactory filtersFactory)
		{
			List<ILogSource> sources = new List<ILogSource>();
			List<IThread> threads = new List<IThread>();

			for (int i = 0; i < items.Count;)
			{
				ScopeItem item = items[i].Key;
				bool isChecked = items[i].Value;

				if (item is AllSources)
				{
					if (isChecked)
					{
						return filtersFactory.CreateScope();
					}
					++i;
					continue;
				}

				if (item is SourceNode)
				{
					if (isChecked)
					{
						sources.Add(((SourceNode)item).Source);
						++i;
						while (i < items.Count && items[i].Key is ThreadNode)
							++i;
					}
					else
					{
						++i;
					}
					continue;
				}

				if (item is ThreadNode)
				{
					if (isChecked)
					{
						threads.Add(((ThreadNode)item).Thread);
					}
					++i;
					continue;
				}

				throw new InvalidOperationException("Unknown node type");
			}

			return filtersFactory.CreateScope(sources, threads);
		}

		MessageFlag GetTypes(List<bool> typesCheckboxesValues)
		{
			MessageFlag f = MessageFlag.None;
			for (int i = 0; i < Math.Min(typeFlagsList.Length, typesCheckboxesValues.Count); ++i)
			{
				if (typesCheckboxesValues[i])
					f |= typeFlagsList[i];
			}
			return f;
		}

		static List<Tuple<FilterAction, string, ModelColor?>> MakeActionsOptions(FiltersListPurpose purpose)
		{
			var actionOptions = new List<Tuple<FilterAction, string, ModelColor?>>();

			string excludeDescription;
			if (purpose == FiltersListPurpose.Highlighting)
				excludeDescription = "Exclude from highlighting";
			else if (purpose == FiltersListPurpose.Search)
				excludeDescription = "Exclude from search results";
			else
				excludeDescription = "Exclude";
			actionOptions.Add(Tuple.Create(FilterAction.Exclude, excludeDescription, new ModelColor?()));

			if (purpose == FiltersListPurpose.Search)
			{
				actionOptions.Add(Tuple.Create(FilterAction.Include, "Include to search result", new ModelColor?()));
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
				actionOptions.Add(Tuple.Create(a, 
					string.Format(includeAndColorizeFormat, a - FilterAction.IncludeAndColorizeFirst + 1), 
					a.GetBackgroundColor()));
			}

			return actionOptions;
		}
	};
};