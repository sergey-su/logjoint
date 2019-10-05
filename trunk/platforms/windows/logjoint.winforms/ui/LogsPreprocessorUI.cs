using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class LogsPreprocessorCredentialsCache : Preprocessing.ICredentialsCache
	{
		readonly Persistence.IStorageEntry credentialsCacheStorage;
		readonly object credentialCacheLock = new object();
		readonly ISynchronizationContext uiInvokeSynchronization;
		readonly Form appWindow;
		NetworkCredentialsStorage credentialCache = null;

		public LogsPreprocessorCredentialsCache(ISynchronizationContext uiInvokeSynchronization, Persistence.IStorageEntry credentialsCacheStorage, Form appWindow)
		{
			this.credentialsCacheStorage = credentialsCacheStorage;
			this.uiInvokeSynchronization = uiInvokeSynchronization;
			this.appWindow = appWindow;
		}

		NetworkCredential Preprocessing.ICredentialsCache.QueryCredentials(Uri uri, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage, new Persistence.SystemDataProtection());
				var cred = credentialCache.GetCredential(uri);
				if (cred != null)
					return cred;
				var ret = uiInvokeSynchronization.Invoke<NetworkCredential>(() =>
					CredUIUtils.ShowCredentialsDialog(appWindow.Handle,
						NetworkCredentialsStorage.GetRelevantPart(uri).ToString(), 
						authType == "protected-archive")).Result;
				if (ret == null)
					return null;
				credentialCache.Add(uri, ret);
				credentialCache.StoreSecurely();
				return ret;
			}
		}

		void Preprocessing.ICredentialsCache.InvalidateCredentialsCache(Uri site, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage, new Persistence.SystemDataProtection());
				if (credentialCache.Remove(site))
					credentialCache.StoreSecurely();
			}
		}

	};

	class LogsPreprocessorUI : Presenters.PreprocessingUserInteractions.IView
	{
		readonly Form appWindow;
		readonly ISynchronizationContext synchronizationContext;
		Windows.Reactive.IReactive reactive;
		FilesSelectionDialog dialog;

		public LogsPreprocessorUI(Form appWindow, ISynchronizationContext synchronizationContext, Windows.Reactive.IReactive reactive)
		{
			this.appWindow = appWindow;
			this.synchronizationContext = synchronizationContext;
			this.reactive = reactive;
		}

		void Presenters.PreprocessingUserInteractions.IView.SetViewModel(Presenters.PreprocessingUserInteractions.IViewModel viewModel)
		{
			var updateDialog = Updaters.Create(
				() => viewModel.DialogData,
				dd => {
					if ((dd != null) != (dialog != null))
					{
						if (dialog != null)
						{
							dialog.Close();
							dialog = null;
						}
						else
						{
							synchronizationContext.Post(() => {
								if (viewModel.DialogData != null)
								{
									appWindow.BringToFront();
									FilesSelectionDialog.Open(viewModel, reactive, out dialog);
								}
							});
						}
					}
					else if (dialog != null && dd != null)
					{
						dialog.Update(dd);
					}
				}
			);
			viewModel.ChangeNotification.CreateSubscription(updateDialog);
		}
	}
}
