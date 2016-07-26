using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	public class DummyModel : IModel
	{
		readonly IModelThreads threads;
		readonly IFiltersList hlFilters = new FiltersList(FilterAction.Exclude);

		public DummyModel(IModelThreads threads = null, Settings.IGlobalSettingsAccessor settings = null, IMessagesCollection messages = null)
		{
			this.threads = threads ?? new ModelThreads();

			hlFilters.FilteringEnabled = false;
		}

		IEnumerable<IMessagesSource> IModel.Sources
		{
			get { return null; } // todo: provide access to messages
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

		event EventHandler IModel.OnSourcesChanged
		{
			add { }
			remove { }
		}

		event EventHandler IModel.OnSourceMessagesChanged
		{
			add { }
			remove { }
		}
	};
};