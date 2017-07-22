using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint
{
	public class FiltersList : IFiltersList, IDisposable
	{
		public FiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			this.actionWhenEmptyOrDisabled = actionWhenEmptyOrDisabled;
		}

		public FiltersList(XElement e, IFiltersFactory factory)
		{
			LoadInternal(e, factory);
		}

		void IDisposable.Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			foreach (var f in list)
			{
				f.Dispose();
			}
			list.Clear();
			OnFiltersListChanged = null;
			OnPropertiesChanged = null;
		}

		IFiltersList IFiltersList.Clone()
		{
			IFiltersList ret = new FiltersList(actionWhenEmptyOrDisabled);
			ret.FilteringEnabled = filteringEnabled;
			foreach (var f in list)
				ret.Insert(ret.Count, f.Clone(f.InitialName));
			return ret;
		}

		bool IFiltersList.FilteringEnabled
		{
			get
			{
				return filteringEnabled;
			}
			set
			{
				if (value == filteringEnabled)
					return;
				filteringEnabled = value;
				OnFilteringEnabledOrDisabled();
			}
		}

		#region Events
		public event EventHandler OnFiltersListChanged;
		public event EventHandler OnFilteringEnabledChanged;
		public event EventHandler<FilterChangeEventArgs> OnPropertiesChanged;
		#endregion

		#region Filters access and manipulation
		IEnumerable<IFilter> IFiltersList.Items
		{
			get { return list; }
		}
		int IFiltersList.Count
		{
			get { return list.Count; }
		}
		void IFiltersList.Insert(int position, IFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException("filter");
			filter.SetOwner(this);
			list.Insert(position, filter);
			OnChanged();
		}
		bool IFiltersList.Move(IFilter f, bool upward)
		{
			int idx = -1;
			if (f.Owner == this)
				idx = list.IndexOf(f);
			if (idx < 0)
				throw new ArgumentException("Filter doesn't belong to this list");

			bool movePossible;
			if (upward)
			{
				if ((movePossible = idx > 0) == true)
					Swap(idx, idx - 1);
			}
			else
			{
				if ((movePossible = idx < list.Count - 1) == true)
					Swap(idx, idx + 1);
			}

			if (movePossible)
				OnChanged();

			return movePossible;
		}
		void IFiltersList.Delete(IEnumerable<IFilter> range)
		{
			int toRemove = 0;
			foreach (var f in range)
			{
				if (f.Owner != this)
					throw new InvalidOperationException("Can not remove the filter that doesn't belong to the list");
				++toRemove;
			}
			if (toRemove == 0)
				return;

			foreach (var f in range)
			{
				list.Remove(f);
				f.SetOwner(null);
				f.Dispose();
			}

			OnChanged();
		}

		int IFiltersList.PurgeDisposedFiltersAndFiltersHavingDisposedThreads()
		{
			int i = ListUtils.RemoveIf(list, 0, list.Count, f => f.IsDisposed || f.Options.Scope.IsDead);

			int itemsToRemove = list.Count - i;
			if (itemsToRemove == 0)
				return itemsToRemove;

			for (int j = i; j < list.Count; ++j)
			{
				if (!list[j].IsDisposed)
				{
					list[j].SetOwner(null);
					list[j].Dispose();
				}
			}
			list.RemoveRange(i, itemsToRemove);

			OnChanged();

			return itemsToRemove;
		}

		void IFiltersList.Save(XElement e)
		{
			SaveInternal(e);
		}
		#endregion

		#region Messages processing

		IFiltersListBulkProcessing IFiltersList.StartBulkProcessing(bool matchRawMessages)
		{
			if (!filteringEnabled)
				return new DummyBulkProcessing(actionWhenEmptyOrDisabled);

			var defAction = ((IFiltersList)this).GetDefaultAction();

			if (list.Count == 0)
				return new DummyBulkProcessing(defAction);

			return new BulkProcessing(matchRawMessages, list, defAction);
		}

		FilterAction IFiltersList.GetDefaultAction()
		{
			if (!defaultAction.HasValue)
			{
				if (list.Count > 0)
				{
					defaultAction = actionWhenEmptyOrDisabled;
					for (int i = list.Count - 1; i >= 0; --i)
					{
						var f = list[i];
						if (!f.IsDisposed && f.Enabled)
						{
							defaultAction = f.Action == FilterAction.Exclude ? FilterAction.Include : FilterAction.Exclude;
							break;
						}
					}
				}
				else
				{
					defaultAction = actionWhenEmptyOrDisabled;
				}
			}
			return defaultAction.Value;
		}

		void IFiltersList.InvalidateDefaultAction()
		{
			InvalidateDefaultActionInternal();
		}

		void IFiltersList.FireOnPropertiesChanged(IFilter sender, bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
		{
			if (OnPropertiesChanged != null)
				OnPropertiesChanged(sender, new FilterChangeEventArgs(changeAffectsFilterResult, changeAffectsPreprocessingResult));
		}

		#endregion

		#region Implementation

		void InvalidateDefaultActionInternal()
		{
			defaultAction = null;
		}

		private void OnChanged()
		{
			InvalidateDefaultActionInternal();
			if (OnFiltersListChanged != null)
				OnFiltersListChanged(this, EventArgs.Empty);
		}

		private void OnFilteringEnabledOrDisabled()
		{
			InvalidateDefaultActionInternal();
			OnFilteringEnabledChanged?.Invoke(this, EventArgs.Empty);
		}

		void Swap(int idx1, int idx2)
		{
			var tmp = list[idx1];
			list[idx1] = list[idx2];
			list[idx2] = tmp;
		}

		void SaveInternal(XElement e)
		{
			e.SetAttributeValue("default-action", (int)actionWhenEmptyOrDisabled);
			foreach (var i in list)
			{
				var f = new XElement("filter");
				i.Save(f);
				e.Add(f);
			}
		}

		void LoadInternal(XElement e, IFiltersFactory factory)
		{
			actionWhenEmptyOrDisabled = (FilterAction)e.SafeIntValue("default-action", (int)FilterAction.Exclude);
			foreach (var f in e.Elements("filter"))
				list.Add(factory.CreateFilter(f));
		}

		#endregion

		#region Members

		bool disposed;
		readonly List<IFilter> list = new List<IFilter>();
		FilterAction actionWhenEmptyOrDisabled;
		FilterAction? defaultAction;
		bool filteringEnabled = true;

		#endregion

		class DummyBulkProcessing : IFiltersListBulkProcessing
		{
			readonly FilterAction action;

			public DummyBulkProcessing(FilterAction action)
			{
				this.action = action;
			}

			void IDisposable.Dispose()
			{
			}

			MessageFilteringResult IFiltersListBulkProcessing.ProcessMessage(IMessage msg, int? startFrom)
			{
				return new MessageFilteringResult()
				{
					Action = action,
				};
			}
		}

		class BulkProcessing : IFiltersListBulkProcessing
		{
			readonly FilterAction defaultAction;
			readonly KeyValuePair<IFilterBulkProcessing, IFilter>[] filters;

			public BulkProcessing(bool matchRawMessages, IEnumerable<IFilter> filters, FilterAction defaultAction)
			{
				this.filters = filters
					.Where(f => f.Enabled)
					.Select(f => new KeyValuePair<IFilterBulkProcessing, IFilter>(
						f.StartBulkProcessing(matchRawMessages), f
					))
					.ToArray();
				this.defaultAction = defaultAction;
			}

			void IDisposable.Dispose()
			{
				foreach (var f in filters)
					f.Key.Dispose();
			}

			MessageFilteringResult IFiltersListBulkProcessing.ProcessMessage(IMessage msg, int? startFromChar)
			{
				for (int i = 0; i < filters.Length; ++i)
				{
					var f = filters[i];
					var m = f.Key.Match(msg, startFromChar);
					if (m != null)
					{
						return new MessageFilteringResult()
						{
							Action = f.Value.Action,
							Filter = f.Value,
							MatchedRange = m
						};
					}
				}

				return new MessageFilteringResult()
				{
					Action = defaultAction,
				};
			}
		};
	}
}
