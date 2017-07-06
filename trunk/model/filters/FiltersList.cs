using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class FiltersList: IFiltersList, IDisposable
	{
		public FiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			this.actionWhenEmptyOrDisabled = actionWhenEmptyOrDisabled;
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
			if (list.Count == FiltersPreprocessingResult.MaxEnabledFiltersSupportedByPreprocessing)
				throw new TooManyFiltersException();
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
			int i = ListUtils.RemoveIf(list, 0, list.Count, f => f.IsDisposed || f.Target.IsDead);

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
		#endregion

		#region Bulk processing
		public FiltersBulkProcessingHandle BeginBulkProcessing()
		{
			FiltersBulkProcessingHandle ret = new FiltersBulkProcessingHandle();		
			return ret;
		}

		void IFiltersList.EndBulkProcessing(FiltersBulkProcessingHandle handle)
		{
		}
		#endregion

		#region Messages processing

		FiltersPreprocessingResult IFiltersList.PreprocessMessage(IMessage msg, bool matchRawMessages)
		{
			UInt64 mask = 0;

			if (filteringEnabled)
			{
				UInt64 nextBitToSet = 1;
				for (int i = 0; i < list.Count; ++i)
				{
					var f = list[i];
					if (f.Match(msg, matchRawMessages))
						mask |= nextBitToSet;
					unchecked { nextBitToSet *= 2; }
				}
			}

			FiltersPreprocessingResult ret;
			ret.mask = mask;
			return ret;
		}

		FilterAction IFiltersList.ProcessNextMessageAndGetItsAction(IMessage msg, FiltersPreprocessingResult preprocessingResult, FilterContext filterCtx, bool matchRawMessages)
		{
			return ProcessNextMessageAndGetItsActionImpl(msg, filterCtx, preprocessingResult.mask, true, matchRawMessages);
		}

		FilterAction IFiltersList.ProcessNextMessageAndGetItsAction(IMessage msg, FilterContext filterCtx, bool matchRawMessages)
		{
			return ProcessNextMessageAndGetItsActionImpl(msg, filterCtx, 0, false, matchRawMessages);
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
			if (OnFilteringEnabledChanged != null)
				OnFilteringEnabledChanged(this, EventArgs.Empty);
		}

		void Swap(int idx1, int idx2)
		{
			var tmp = list[idx1];
			list[idx1] = list[idx2];
			list[idx2] = tmp;
		}

		FilterAction ProcessNextMessageAndGetItsActionImpl(IMessage msg, FilterContext filterCtx, UInt64 mask, bool maskValid, bool matchRawMessages)
		{
			if (!filteringEnabled)
			{
				return actionWhenEmptyOrDisabled;
			}
			IThread thread = msg.Thread;
			var regionFilter = filterCtx.RegionFilter;
			if (regionFilter == null)
			{
				UInt64 nextMaskBitToCheck = 1;
				for (int i = 0; i < list.Count; ++i)
				{
					var f = list[i];
					if (f.Enabled)
					{
						bool match;
						if (maskValid)
							match = (mask & nextMaskBitToCheck) != 0;
						else
							match = f.Match(msg, matchRawMessages);
						if (match)
						{
							if (f.MatchFrameContent && (msg.Flags & MessageFlag.StartFrame) != 0)
							{
								filterCtx.BeginRegion(f);
							}
							return f.Action;
						}
					}
					if (maskValid)
					{
						unchecked { nextMaskBitToCheck *= 2; }
					}
				}
			}
			else
			{
				switch (msg.Flags & MessageFlag.TypeMask)
				{
					case MessageFlag.StartFrame:
						filterCtx.BeginRegion(regionFilter);
						break;
					case MessageFlag.EndFrame:
						filterCtx.EndRegion();
						break;
				}
				return regionFilter.Action;
			}

			return ((IFiltersList)this).GetDefaultAction();
		}

		#endregion

		#region Members

		bool disposed;
		readonly List<IFilter> list = new List<IFilter>();
		readonly FilterAction actionWhenEmptyOrDisabled;
		FilterAction? defaultAction;
		bool filteringEnabled = true;

		#endregion
	}
}
