using System;
using System.ComponentModel;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Threading;

namespace LogJoint.UI
{
	public class NSSynchronizeInvoke : NSObject, ISynchronizeInvoke
	{
		IAsyncResult ISynchronizeInvoke.BeginInvoke (Delegate method, object[] args)
		{
			var ret = new AsyncResult();
			BeginInvokeOnMainThread (() => 
			{
				object result;
				try
				{
					result = method.DynamicInvoke (args);
					ret.Set(result, null);
				}
				catch (Exception e)
				{
					ret.Set(null, e);
				}
			});
			return ret;
		}

		object ISynchronizeInvoke.EndInvoke (IAsyncResult result)
		{
			return ((AsyncResult)result).Get();
		}

		object ISynchronizeInvoke.Invoke (Delegate method, object[] args)
		{
			object ret = null;
			InvokeOnMainThread (() => { ret = method.DynamicInvoke (args); });
			return ret;
		}

		bool ISynchronizeInvoke.InvokeRequired 
		{			
			get { return !NSThread.IsMain; }
		}

		class AsyncResult: IAsyncResult, IDisposable
		{
			readonly ManualResetEvent e = new ManualResetEvent(false);
			object result;
			Exception exception;

			public void Set(object ret, Exception ex)
			{
				result = ret;
				exception = ex;
				e.Set();
			}

			public object Get()
			{
				if (exception != null)
					throw exception;
				return result;
			}

			void IDisposable.Dispose()
			{
				e.Dispose();
			}
				
			object IAsyncResult.AsyncState
			{
				get	{ return null; 	}
			}
			WaitHandle IAsyncResult.AsyncWaitHandle
			{
				get	{ return e; }
			}
			bool IAsyncResult.CompletedSynchronously
			{
				get	{ return false; }
			}
			bool IAsyncResult.IsCompleted
			{
				get	{ return e.WaitOne(0); }
			}
		};
	}
}