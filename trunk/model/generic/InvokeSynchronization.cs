using System;
using System.Threading;

namespace LogJoint
{
	public interface IInvokeSynchronization
	{
		bool InvokeRequired { get; }
		IAsynchronousInvokeResult BeginInvoke(Delegate method, object[] args);
		object EndInvoke(IAsynchronousInvokeResult result);
		object Invoke(Delegate method, object[] args);
	}

	public interface IAsynchronousInvokeResult
	{
		WaitHandle AsyncWaitHandle { get; }
		bool CompletedSynchronously { get; }
		bool IsCompleted { get; }
	}

}
