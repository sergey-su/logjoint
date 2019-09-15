using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.WebViewTools
{
	public interface IPresenter
	{
	};

	public interface IView
	{
		void SetViewModel(IViewModel handler);
		bool Visible { get; set; }
		void Navigate(Uri uri);
	};

	/// <summary>
	/// Can be called from non-UI threads
	/// </summary>
	public interface IViewModel
	{
		bool OnStartDownload(Uri uri);
		bool OnProgress(int currentValue, int totalSize, string statusText);
		bool OnDataAvailable(byte[] buffer, int bytesAvailable);
		void OnDownloadCompleted(bool success, string statusText);
		void OnAborted();
		void OnBrowserNavigated(Uri url);
		void OnFormSubmitted(IReadOnlyList<KeyValuePair<string, string>> values);
		WebViewAction CurrentAction { get; }
	};

	public enum WebViewActionType
	{
		Download,
		UploadForm
	};

	public class WebViewAction
	{
		public WebViewActionType Type { get; internal set; }
		public Uri Uri { get; internal set; }
		public string MimeType { get; internal set; }
	};
};