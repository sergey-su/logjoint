using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class DummyModel : IModel
	{
		readonly IModelThreads threads;
		readonly IMessagesCollection messages;
		readonly IFiltersList displayFilters = new FiltersList(FilterAction.Include);
		readonly IFiltersList hlFilters = new FiltersList(FilterAction.Exclude);

		public DummyModel(IModelThreads threads = null, IMessagesCollection messages = null, Settings.IGlobalSettingsAccessor settings = null)
		{
			this.threads = threads ?? new ModelThreads();
			this.messages = messages ?? new MessagesContainers.RangesManagingCollection();

			displayFilters.FilteringEnabled = false;
			hlFilters.FilteringEnabled = false;
		}

		IMessagesCollection IModel.Messages
		{
			get { return messages; }
		}

		IModelThreads IModel.Threads
		{
			get { return threads; }
		}

		IFiltersList IModel.DisplayFilters
		{
			get { return displayFilters; }
		}

		IFiltersList IModel.HighlightFilters
		{
			get { return hlFilters; }
		}

		IBookmarks IModel.Bookmarks
		{
			get { return null; }
		}

		LJTraceSource IModel.Tracer
		{
			get { return LJTraceSource.EmptyTracer; }
		}

		string IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get { return null; }
		}

		void IModel.ShiftUp()
		{
		}

		bool IModel.IsShiftableUp
		{
			get { return false; }
		}

		void IModel.ShiftDown()
		{
		}

		bool IModel.IsShiftableDown
		{
			get { return false; }
		}

		void IModel.ShiftAt(System.DateTime t)
		{
		}

		void IModel.ShiftHome()
		{
		}

		void IModel.ShiftToEnd()
		{
		}

		bool IModel.GetAndResetPendingUpdateFlag()
		{
			return true;
		}

		Settings.IGlobalSettingsAccessor IModel.GlobalSettings
		{
			get { return Settings.DefaultSettingsAccessor.Instance; }
		}

		event EventHandler<MessagesChangedEventArgs> IModel.OnMessagesChanged
		{
			add {}
			remove {}
		}
	};
};