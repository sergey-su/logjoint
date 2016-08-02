using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class DummyModel : IModel
	{
		readonly IModelThreads threads;
		readonly IFiltersList hlFilters;
		readonly DummySource dummySource;

		public DummyModel(IModelThreads threads = null, Settings.IGlobalSettingsAccessor settings = null)
		{
			this.threads = threads ?? new ModelThreads();
			this.dummySource = new DummySource();
			this.hlFilters = new FiltersList(FilterAction.Exclude);
			hlFilters.FilteringEnabled = false;
		}

		public void SetMessages(IEnumerable<IMessage> msgs)
		{
			dummySource.messages.Clear();
			foreach (var m in msgs)
				dummySource.messages.Add(m);
			if (OnSourcesChanged != null)
				OnSourcesChanged(this, EventArgs.Empty);
		}

		public event EventHandler OnSourcesChanged;

		IEnumerable<IMessagesSource> IModel.Sources
		{
			get { yield return dummySource; }
		}

		IModelThreads IModel.Threads
		{
			get { return threads; }
		}

		IFiltersList IModel.HighlightFilters
		{
			get { return hlFilters; }
		}

		IBookmarks IModel.Bookmarks
		{
			get { return null; }
		}

		string IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get { return null; }
		}

		Settings.IGlobalSettingsAccessor IModel.GlobalSettings
		{
			get { return Settings.DefaultSettingsAccessor.Instance; }
		}

		event EventHandler IModel.OnLogSourceColorChanged
		{
			add { }
			remove { }
		}

		event EventHandler IModel.OnSourceMessagesChanged
		{
			add { }
			remove { }
		}

		class DummySource : LogViewer.IMessagesSource
		{
			public MessagesContainers.ListBasedCollection messages = new MessagesContainers.ListBasedCollection();

			Task<DateBoundPositionResponseData> IMessagesSource.GetDateBoundPosition(DateTime d, ListUtils.ValueBound bound, LogProviderCommandPriority priority, System.Threading.CancellationToken cancellation)
			{
				return Task.FromResult(messages.GetDateBoundPosition(d, bound));
			}

			Task IMessagesSource.EnumMessages(long fromPosition, Func<IndexedMessage, bool> callback, 
				EnumMessagesFlag flags, LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				messages.EnumMessages(fromPosition, callback, flags);
				return Task.FromResult(0);
			}

			FileRange.Range IMessagesSource.PositionsRange
			{
				get { return messages.PositionsRange; }
			}

			DateRange IMessagesSource.DatesRange
			{
				get { return messages.DatesRange; }
			}

			FileRange.Range IMessagesSource.IndexesRange
			{
				get { return messages.IndexesRange; }
			}
		};
	};
};