using NUnit.Framework;
using System;
using System.Threading.Tasks;
using FakeTimeProvider = Microsoft.Extensions.Time.Testing.FakeTimeProvider;

namespace LogJoint.Tests
{
    [TestFixture]
    public class AsyncInvokeTest
    {
        [Test]
        public void SchedulesTheMethod()
        {
            var sync = new ManualSynchronizationContext();
            int methodCalled = 0;
            var helper = new AsyncInvokeHelper(sync, () => methodCalled++);

            helper.Invoke();
            Assert.That(methodCalled, Is.EqualTo(0));
            sync.Deplete();
            Assert.That(methodCalled, Is.EqualTo(1));
        }

        [Test]
        public void SchedulesAtMostOneMethod()
        {
            var sync = new ManualSynchronizationContext();
            int methodCalled = 0;
            var helper = new AsyncInvokeHelper(sync, () => methodCalled++);

            helper.Invoke();
            helper.Invoke();
            helper.Invoke();

            Assert.That(methodCalled, Is.EqualTo(0));
            sync.Deplete();
            Assert.That(methodCalled, Is.EqualTo(1));
        }

        [Test]
        public async Task SchedulesWithThrottling()
        {
            var sync = new SerialSynchronizationContext();
            var time = new FakeTimeProvider();
            int methodCalled = 0;
            await sync.Invoke(async () =>
            {
                var invoke = new AsyncInvokeHelper(sync, () => methodCalled++, time).CreateThrottlingInvoke(TimeSpan.FromSeconds(1));

                // Schedules immediately initially.
                invoke();
                await Task.Yield();
                Assert.That(methodCalled, Is.EqualTo(1));

                // Time passed, nothing happens.
                time.Advance(TimeSpan.FromMicroseconds(500));
                await Task.Yield();
                Assert.That(methodCalled, Is.EqualTo(1));

                // Not enough time passed since last invoke.
                invoke();
                await Task.Yield();
                Assert.That(methodCalled, Is.EqualTo(1));

                // Still not enough time passed since last invoke.
                // New request is agnored.
                invoke();
                await Task.Yield();
                Assert.That(methodCalled, Is.EqualTo(1));

                // Enough time passes.
                time.Advance(TimeSpan.FromMilliseconds(500));
                await Task.Yield();
                Assert.That(methodCalled, Is.EqualTo(2));
            });
        }
    }
}
