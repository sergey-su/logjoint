using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IModel
	{
		IMessagesCollection Messages { get; }
		IThreads Threads { get; }
		FiltersList DisplayFilters { get; }
		FiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		IUINavigationHandler UINavigationHandler { get; }
		LJTraceSource Tracer { get; }
		string MessageToDisplayWhenMessagesCollectionIsEmpty { get; }
		void ShiftUp();
		bool IsShiftableUp { get; }
		void ShiftDown();
		bool IsShiftableDown { get; }
		void ShiftAt(DateTime t);
		void ShiftHome();
		void ShiftToEnd();

		event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
	};
};