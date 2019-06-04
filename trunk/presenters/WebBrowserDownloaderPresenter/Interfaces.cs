using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.WebBrowserDownloader
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
		CurrentWebDownloadTarget CurrentTarget { get; }
	};

	public class CurrentWebDownloadTarget
	{
		public Uri Uri { get; internal set; }
		public string MimeType { get; internal set; }
	};
};