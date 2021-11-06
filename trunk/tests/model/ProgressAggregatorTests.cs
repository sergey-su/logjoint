using System;
using System.Text;
using System.Collections.Generic;
using LogJoint.Progress;
using LogJoint;
using NSubstitute;
using System.ComponentModel;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class ProgressAggregatorTests
	{
		TaskCompletionSource<int> lastSleep;
		ISynchronizationContext invoke;
		IProgressAggregator agg;
		IOutEvents outEvents;
		Action lastInvokedAction;

		public interface IOutEvents
		{
			void ProgressStarted(object sender, EventArgs e);
			void ProgressChanged(object sender, ProgressChangedEventArgs e);
			void ProgressEnded(object sender, EventArgs e);
		};

		static IOutEvents MakeOutEventsMock(IProgressAggregator agg)
		{
			var outEvents = Substitute.For<IOutEvents>();
			agg.ProgressStarted += (s, e) => outEvents.ProgressStarted(s, e);
			agg.ProgressChanged += (s, e) => outEvents.ProgressChanged(s, e);
			agg.ProgressEnded += (s, e) => outEvents.ProgressEnded(s, e);
			return outEvents;
		}

		[SetUp]
		public void Init()
		{
			invoke = Substitute.For<ISynchronizationContext>();
			invoke.When(x => x.Post(Arg.Any<Action>())).Do(callInfo => 
			{
				Assert.IsNull(lastInvokedAction);
				lastInvokedAction = callInfo.Arg<Action>();
			});
			agg = ((IProgressAggregatorFactory)new ProgressAggregator.Factory(invoke, delay =>
			{
				Assert.IsNull(lastSleep);
				lastSleep = new TaskCompletionSource<int>();
				return lastSleep.Task;
			})).CreateProgressAggregator();
			outEvents = MakeOutEventsMock(agg);
		}

		[TearDown]
		public void Shutdown()
		{
			lastInvokedAction = null;
			lastSleep = null;
		}

		void HeartBeat()
		{
			Assert.IsNotNull(lastSleep);
			var sleepToComplete = lastSleep;
			lastSleep = null;
			sleepToComplete.SetResult(1);
		}

		void RunInvokedAction()
		{
			Assert.IsNotNull(lastInvokedAction);
			var actionToComplete = lastInvokedAction;
			lastInvokedAction = null;
			actionToComplete();
		}

		[Test]
		public void InitialPropertiesStayUnchangedIfNoContributrosCreated()
		{
			Assert.AreEqual(null, agg.ProgressValue);
			Assert.IsNull(lastInvokedAction);
			Assert.IsNull(lastSleep);
		}

		[Test]
		public void TestOneContributingSink()
		{
			using (var sink = agg.CreateProgressSink())
			{
				RunInvokedAction(); // run periodic
				HeartBeat();
				outEvents.Received().ProgressStarted(agg, Arg.Any<EventArgs>());
				Assert.AreEqual(0d, agg.ProgressValue);

				sink.SetValue(0);
				HeartBeat();
				outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
				Assert.AreEqual(0d, agg.ProgressValue);

				sink.SetValue(0.1d);
				HeartBeat();
				outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
				Assert.AreEqual(0.1d, agg.ProgressValue);

				sink.SetValue(0.9d);
				HeartBeat();
				outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
				Assert.AreEqual(0.9d, agg.ProgressValue);
			}
			RunInvokedAction();
			Assert.AreEqual(null, agg.ProgressValue);
			outEvents.Received().ProgressEnded(agg, Arg.Any<EventArgs>());
		}

		[Test]
		public void TestTwoContributingSinks()
		{
			using (var sink1 = agg.CreateProgressSink())
			{
				using (var sink2 = agg.CreateProgressSink())
				{
					RunInvokedAction(); // run periodic
					sink1.SetValue(0.5d);
					sink2.SetValue(0.1d);
					HeartBeat();
					outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
					Assert.AreEqual(0.3d, agg.ProgressValue);

					sink1.SetValue(0.6d);
					sink2.SetValue(0.8d);
					HeartBeat();
					outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
					Assert.AreEqual(0.7d, agg.ProgressValue);
				}
				RunInvokedAction();
				// sink1 stays 0.6, disposed sink2 becomes 1.0
				// aggregated value becomes (0.6 + 1.0)/2
				Assert.AreEqual(0.8, agg.ProgressValue);
			}
			RunInvokedAction();
			Assert.AreEqual(null, agg.ProgressValue);
			outEvents.Received().ProgressEnded(agg, Arg.Any<EventArgs>());
		}

		[Test]
		public void TestOneSinkPlusOneChildAggregator()
		{
			using (var sink = agg.CreateProgressSink())
			{
				using (var childAgg = agg.CreateChildAggregator())
				{
					RunInvokedAction(); // run periodic
					var childAggEvts = MakeOutEventsMock(childAgg);

					sink.SetValue(0.4);
					HeartBeat();
					outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
					Assert.AreEqual(0.2d, agg.ProgressValue); // (sink=0.4 + childAgg=0) / 2

					using (var sink2 = childAgg.CreateProgressSink())
					{
						HeartBeat();
						childAggEvts.Received().ProgressStarted(childAgg, Arg.Any<EventArgs>());
						Assert.AreEqual(0.2d, agg.ProgressValue); // childAgg's value stays 0 in spite of sink2's creation

						sink.SetValue(0.6);
						sink2.SetValue(0.8);
						HeartBeat();
						outEvents.Received().ProgressChanged(agg, Arg.Any<ProgressChangedEventArgs>());
						childAggEvts.Received().ProgressChanged(childAgg, Arg.Any<ProgressChangedEventArgs>());
						Assert.AreEqual(0.7d, agg.ProgressValue);
						Assert.AreEqual(0.8d, childAgg.ProgressValue);
					}
					RunInvokedAction();
					childAggEvts.Received().ProgressEnded(childAgg, Arg.Any<EventArgs>());
					Assert.AreEqual(null, childAgg.ProgressValue);
				}
				RunInvokedAction();
				// sink1 stays 0.6, disposed childAgg becomes 1.0
				// aggregated value becomes (0.6 + 1.0)/2
				Assert.AreEqual(0.8, agg.ProgressValue);
			}
			RunInvokedAction();
			Assert.AreEqual(null, agg.ProgressValue);
			outEvents.Received().ProgressEnded(agg, Arg.Any<EventArgs>());
		}

		[Test]
		public void TestTwoChildAggregatorsCompletedByDispose()
		{
			IOutEvents childAgg1Evts, childAgg2Evts;
			using (var childAgg1 = agg.CreateChildAggregator())
			{
				using (var childAgg2 = agg.CreateChildAggregator())
				{
					RunInvokedAction(); // run periodic

					childAgg1Evts = MakeOutEventsMock(childAgg1);
					childAgg2Evts = MakeOutEventsMock(childAgg2);

					HeartBeat();
					outEvents.Received().ProgressStarted(agg, Arg.Any<EventArgs>());
					Assert.AreEqual(0, agg.ProgressValue);
				}
				RunInvokedAction();
				Assert.AreEqual(0.5d, agg.ProgressValue);
			}
			RunInvokedAction();
			
			Assert.AreEqual(null, agg.ProgressValue);
			outEvents.Received().ProgressEnded(agg, Arg.Any<EventArgs>());

			childAgg1Evts.DidNotReceiveWithAnyArgs().ProgressStarted(null, null);
			childAgg1Evts.DidNotReceiveWithAnyArgs().ProgressEnded(null, null);
			childAgg2Evts.DidNotReceiveWithAnyArgs().ProgressStarted(null, null);
			childAgg2Evts.DidNotReceiveWithAnyArgs().ProgressEnded(null, null);
		}

		[Test]
		public void ChildAggregatorCompletedByDisposedSink()
		{
			using (var childAgg = agg.CreateChildAggregator())
			{
				RunInvokedAction(); // run periodic

				using (var sink = childAgg.CreateProgressSink())
					sink.SetValue(0.5);
				RunInvokedAction();
				Assert.AreEqual(1d, agg.ProgressValue);
				Assert.AreEqual(null, childAgg.ProgressValue);
			}
			RunInvokedAction();
			Assert.AreEqual(null, agg.ProgressValue);
		}

	}
}
