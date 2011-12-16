using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogJoint;

namespace logjoint.model.tests
{
	[TestClass]
	public class SpikeUnitTest
	{
		class Strand
		{
			public void QueueCallback(Action item)
			{
				callbacksQueue.Add(item);
				MakeSureWorkItemIsSubmittedIfNeeded();
			}

			void MakeSureWorkItemIsSubmittedIfNeeded()
			{
				//Barrier()
				if (callbacksQueue.Count > 0)
				{
					// Barrier()
					if (Interlocked.Exchange(ref workitemSubmitted, 1) == 0)
					{
						ThreadPool.QueueUserWorkItem(state =>
						{
							for (int itemsToProcess = callbacksQueue.Count; itemsToProcess > 0; --itemsToProcess)
								// Barrier()
								callbacksQueue.Take()();
							Assert.AreEqual(1, Interlocked.Exchange(ref workitemSubmitted, 0));
							MakeSureWorkItemIsSubmittedIfNeeded();
						});
					}
				}
			}

			BlockingCollection<Action> callbacksQueue = new BlockingCollection<Action>(1024);
			int workitemSubmitted;
		};

		void DoSomeDummyWork()
		{
			Enumerable.Range(0, 10000).Sum();
		}

		[TestMethod]
		public void SpikeUnitTest1()
		{
			var timer = new Stopwatch();
			var strand1 = new Strand();
			var strand2 = new Strand();
			int callbacksCount = 100000;

			int concurrentCallbacks1 = 0;
			int executedCallbacks1 = 0;
			int concurrentCallbacks2 = 0;
			int executedCallbacks2 = 0;

			timer.Start();

			for (int i = 0; i < callbacksCount; ++i)
			{
				strand1.QueueCallback(() =>
				{
					Assert.AreEqual(1, Interlocked.Increment(ref concurrentCallbacks1));
					DoSomeDummyWork();
					++executedCallbacks1;
					Assert.AreEqual(0, Interlocked.Decrement(ref concurrentCallbacks1));
				});
				strand2.QueueCallback(() =>
				{
					Assert.AreEqual(1, Interlocked.Increment(ref concurrentCallbacks2));
					DoSomeDummyWork();
					++executedCallbacks2;
					Assert.AreEqual(0, Interlocked.Decrement(ref concurrentCallbacks2));
				});
			}

			while (executedCallbacks1 != callbacksCount || executedCallbacks2 != callbacksCount)
				Thread.Sleep(10);

			timer.Stop();
			Console.WriteLine("Concurrent execution: {0}", timer.Elapsed);

			timer.Reset();
			timer.Start();
			for (int i = 0; i < callbacksCount; ++i)
			{
				DoSomeDummyWork();
				DoSomeDummyWork();
			}
			timer.Stop();
			Console.WriteLine("Sync execution: {0}", timer.Elapsed);
		}

	}
}
