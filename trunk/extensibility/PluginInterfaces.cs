using System;
using System.Collections.Generic;
using System.Text;

#if WIN
using System.Windows.Forms;
#elif MONOMAC
using MonoMac.AppKit;
#endif

namespace LogJoint
{
	// todo: refactor to expose clear object model
	public interface ILogJointApplication
	{
		IModel Model { get; }
		IInvokeSynchronization UIInvokeSynchronization { get; }

		Telemetry.ITelemetryCollector Telemetry { get; }

		// below is UI related stuff. todo: develop and expose presenters interfaces
		IMessage FocusedMessage { get; }
		IMessagesCollection LoadedMessagesCollection { get; }
		void SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified);
		UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get; }
		UI.Presenters.IPresentersFacade PresentersFacade { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }

		event EventHandler FocusedMessageChanged;
		event EventHandler SourcesChanged;

		#if WIN
		void RegisterToolForm(Form f);
		UI.ILogProviderUIsRegistry LogProviderUIsRegistry { get; }
		#endif
	};

	public class PluginBase : IDisposable
	{
		public virtual void Init(ILogJointApplication app) { }
		public virtual IEnumerable<IMainFormTabExtension> MainFormTabExtensions { get { yield break; } }
		public virtual void Dispose() { }
	};

	public interface IMainFormTabExtension
	{
		#if WIN
		Control PageControl { get; }
		#elif MONOMAC
		NSView PageControl { get; }
		#endif
		string Caption { get; }
		void OnTabPageSelected();
	};
}
