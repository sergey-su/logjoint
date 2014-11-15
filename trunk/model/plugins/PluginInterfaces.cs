using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	public interface IMainFormTabExtension
	{
		Control TapPage { get; }
		string Caption { get; }
	};

	public interface ILogJointApplication
	{
		IModel Model { get; }

		// below is UI related stuff. todo: develop and expose presenters interfaces
		MessageBase FocusedMessage { get; }
		IMessagesCollection LoadedMessagesCollection { get; }
		void RegisterToolForm(Form f);
		void SelectMessageAt(IBookmark bmk, Predicate<MessageBase> messageMatcherWhenNoHashIsSpecified);
		void ShowFilter(IFilter f);

		event EventHandler FocusedMessageChanged;
		event EventHandler SourcesChanged;
	};

	public class PluginBase : IDisposable
	{
		public virtual void Init(ILogJointApplication app) { }
		public virtual IEnumerable<IMainFormTabExtension> MainFormTagExtensions { get { yield break; } }
		public virtual void Dispose() { }
	};
}
