using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.FilterDialog
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly ILogSourcesManager logSources;
		List<ScopeItem> scopeItems;
		bool clickLock;
		IFilter tempFilter;
		static readonly MessageFlag[] typeFlagsList = 
		{
			MessageFlag.Error | MessageFlag.Content,
			MessageFlag.Warning | MessageFlag.Content,
			MessageFlag.Info | MessageFlag.Content
		};

		public Presenter(ILogSourcesManager logSources, IFiltersList filtersList, IView view)
		{
			this.logSources = logSources;
			this.view = view;
			view.SetEventsHandler(this);
		}

		bool IPresenter.ShowTheDialog(IFilter forFilter)
		{
			var filter = forFilter;
			using (tempFilter = forFilter.Clone(forFilter.InitialName))
			{
				WriteView(filter);
				if (!view.ShowDialog())
					return false;
				ReadView(filter);
				return true;
			}
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
			RefreshNameTextBox();
		}

		void RefreshNameTextBox()
		{
			ReadView(tempFilter);
			if (tempFilter.Name != view.GetData().NameEditValue)
				view.SetNameEditValue(tempFilter.Name);
		}

		List<KeyValuePair<ScopeItem, bool>> CreateScopeItems(IFilterScope target)
		{
			var items = new List<KeyValuePair<ScopeItem, bool>>();

			Action<ScopeItem, bool> add = (i, isChecked) => items.Add(new KeyValuePair<ScopeItem, bool>(i, isChecked));

			bool matchesAllSources = target.ContainsEverything;
			add(new AllSources(items.Count, this), matchesAllSources);

			foreach (ILogSource s in logSources.Items)
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
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				yield return (typeFlagsList[i] & filter.Options.TypesToLookFor) == typeFlagsList[i];
			}
		}

		abstract class Node: ScopeItem
		{
			public Node(int idx, Presenter owner)
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
			var scopeItems = CreateScopeItems(filter.Options.Scope ?? filter.Factory.CreateScope());
			this.scopeItems = scopeItems.Select(i => i.Key).ToList();
			clickLock = true;
			view.SetData(
				"Highlight Filter",
				new[]
				{
						"Highlight",
						"Exclude from highlighting"
				},
				new[]
				{
						"Errors",
						"Warnings",
						"Infos"
				},
				new DialogValues()
				{
					NameEditValue = filter.Name,
					EnabledCheckboxValue = filter.Enabled,
					TemplateEditValue = filter.Options.Template,
					MatchCaseCheckboxValue = filter.Options.MatchCase,
					RegExpCheckBoxValue = filter.Options.Regexp,
					WholeWordCheckboxValue = filter.Options.WholeWord,
					ActionComboBoxValue = (int)filter.Action,
					ScopeItems = scopeItems,
					TypesCheckboxesValues = CreateTypes(filter).ToList()
				}
			);
			clickLock = false;
		}

		void ReadView(IFilter filter)
		{
			var data = view.GetData();

			filter.SetUserDefinedName(data.NameEditValue);
			filter.Action = (FilterAction)data.ActionComboBoxValue;
			filter.Enabled = data.EnabledCheckboxValue;
			filter.Options = new Search.Options()
			{
				Template = data.TemplateEditValue,
				MatchCase = data.MatchCaseCheckboxValue,
				Regexp = data.RegExpCheckBoxValue,
				WholeWord = data.WholeWordCheckboxValue,
				Scope = CreateScope(data.ScopeItems, filter.Factory),
				TypesToLookFor = GetTypes(data.TypesCheckboxesValues)
			};
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
			for (int i = 0; i < typeFlagsList.Length; ++i)
			{
				if (typesCheckboxesValues[i])
					f |= typeFlagsList[i];
			}
			return f;
		}
	};
};