using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using AppKit;
using LogJoint.UI.Presenters.PreprocessingUserInteractions;

namespace LogJoint.UI
{
	class PreprocessingCredentialsCache: Preprocessing.ICredentialsCache
	{
		readonly Persistence.IStorageEntry credentialsCacheStorage;
		readonly object credentialCacheLock = new object();
		readonly ISynchronizationContext uiInvoke;
		NetworkCredentialsStorage credentialCache = null;
		NSWindow parentWindow;

		public PreprocessingCredentialsCache(
			NSWindow parentWindow,
			Persistence.IStorageEntry credentialsCacheStorage,
			ISynchronizationContext uiInvoke)
		{
			this.credentialsCacheStorage = credentialsCacheStorage;
			this.parentWindow = parentWindow;
			this.uiInvoke = uiInvoke;
		}

		NetworkCredential Preprocessing.ICredentialsCache.QueryCredentials(Uri uri, string authType)
		{
			lock (credentialCacheLock)
			{
				if (credentialCache == null)
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				var cred = credentialCache.GetCredential(uri);
				if (cred != null)
					return cred;
				var ret = uiInvoke.Invoke<NetworkCredential>(() =>
					NetworkCredentialsDialogController.ShowSheet(parentWindow,
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
					credentialCache = new NetworkCredentialsStorage(credentialsCacheStorage);
				if (credentialCache.Remove(site))
					credentialCache.StoreSecurely();
			}
		}
	};

	class LogsPreprocessorUI : IView
	{
		readonly ISynchronizationContext synchronizationContext;
		readonly Mac.IReactive reactive;
		FilesSelectionDialogController dialog;

		public LogsPreprocessorUI (ISynchronizationContext synchronizationContext, Mac.IReactive reactive)
		{
			this.synchronizationContext = synchronizationContext;
			this.reactive = reactive;
		}

		void IView.SetViewModel (IViewModel viewModel)
		{
			var updateDialog = Updaters.Create (
				() => viewModel.DialogData,
				dd => {
					if ((dd != null) != (dialog != null)) {
						if (dialog != null) {
							dialog.Close ();
							dialog = null;
						} else {
							synchronizationContext.Post (() => {
								if (viewModel.DialogData != null)
									FilesSelectionDialogController.Execute (viewModel, reactive, out dialog);
							});
						}
					} else if (dialog != null && dd != null) {
						dialog.Update (dd);
					}
				}
			);
			viewModel.ChangeNotification.CreateSubscription(updateDialog);
		}
	}
}
