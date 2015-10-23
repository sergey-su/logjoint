using LogJoint.UI;
using LogJoint.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Text;
#if WIN
using System.Windows.Forms;
#endif

namespace LogJoint
{
	class LogJointApplication: ILogJointApplication
	{
		public LogJointApplication(IModel model,
			#if WIN
			UI.MainForm mainForm,
			#endif
			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.FiltersListBox.IPresenter filtersPresenter,
			UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter,
			UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter,
			UI.Presenters.IPresentersFacade presentersFacade,
			IInvokeSynchronization uiInvokeSynchronization,
			Telemetry.ITelemetryCollector telemetry,
			Persistence.IWebContentCache webContentCache,
			Persistence.IStorageManager storageManager,
			Extensibility.IPresentation presentation
			#if WIN
			, UI.ILogProviderUIsRegistry logProviderUIsRegistry
			#endif
		)
		{
			this.model = model;
			#if WIN
			this.mainForm = mainForm;
			this.logProviderUIsRegistry = logProviderUIsRegistry;
			#endif
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.messagesPresenter = loadedMessagesPresenter.LogViewerPresenter;
			this.filtersPresenter = filtersPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.presentersFacade = presentersFacade;
			this.uiInvokeSynchronization = uiInvokeSynchronization;
			this.telemetry = telemetry;
			this.webContentCache = webContentCache;
			this.storageManager = storageManager;
			this.presentation = presentation;

			sourcesManagerPresenter.OnViewUpdated += (s, e) =>
			{
				this.FireSourcesChanged();
			};
			messagesPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				this.FireFocusedMessageChanged();
			};
		}

		#region ILogJointApplication Members

		Extensibility.IPresentation ILogJointApplication.Presentation { get { return presentation; } }

		public IModel Model
		{
			get { return model; }
		}

		public IInvokeSynchronization UIInvokeSynchronization
		{
			get { return uiInvokeSynchronization; }
		}

		public Telemetry.ITelemetryCollector Telemetry { get { return telemetry; } }

		#if WIN
		public void RegisterToolForm(Form f)
		{
			IWinFormsComponentsInitializer intf = mainForm;
			intf.InitOwnedForm(f, false);
		}
		#endif

		public IMessage FocusedMessage
		{
			get { return messagesPresenter.FocusedMessage; }
		}

		public IMessagesCollection LoadedMessagesCollection
		{
			get { return messagesPresenter.LoadedMessages; }
		}

		public void SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified)
		{
			bookmarksManagerPresenter.NavigateToBookmark(bmk, messageMatcherWhenNoHashIsSpecified, 
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
		}

		public UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get { return loadedMessagesPresenter; } }

		public UI.Presenters.IPresentersFacade PresentersFacade { get { return presentersFacade; } }

		Persistence.IWebContentCache ILogJointApplication.WebContentCache { get { return webContentCache; } }

		Persistence.IStorageManager ILogJointApplication.StorageManager { get { return storageManager; } }

		#if WIN
		UI.ILogProviderUIsRegistry ILogJointApplication.LogProviderUIsRegistry { get { return logProviderUIsRegistry; } }
		#endif

		public event EventHandler FocusedMessageChanged;
		public event EventHandler SourcesChanged;

		#endregion

		void FireFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		void FireSourcesChanged()
		{
			if (SourcesChanged != null)
				SourcesChanged(this, EventArgs.Empty);
		}

		IModel model;
		#if WIN
		UI.MainForm mainForm;
		UI.ILogProviderUIsRegistry logProviderUIsRegistry;
		#endif
		UI.Presenters.LogViewer.IPresenter messagesPresenter;
		UI.Presenters.FiltersListBox.IPresenter filtersPresenter;
		UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter;
		UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter;
		UI.Presenters.IPresentersFacade presentersFacade;
		IInvokeSynchronization uiInvokeSynchronization;
		Telemetry.ITelemetryCollector telemetry;
		Persistence.IWebContentCache webContentCache;
		Persistence.IStorageManager storageManager;
		Extensibility.IPresentation presentation;
	}
}
