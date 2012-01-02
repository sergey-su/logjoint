using LogJoint.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhino.Mocks;

namespace logjoint.model.tests
{
	[TestClass()]
	public class StorageManagerTest
	{
		public class TestException : Exception { };

		static MemoryStream CreateTextStream(string s)
		{
			return new MemoryStream(Encoding.ASCII.GetBytes(s));
		}

		[TestMethod()]
		[ExpectedException(typeof(TestException))]
		public void StorageManager_AutoCleanup_StogareInfoAccessFailureFailsTheConstructor()
		{
			MockRepository repo = new MockRepository();

			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();
			Expect.Call(impl.OpenFile("cleanup.info", false)).Throw(new TestException());
			repo.ReplayAll();
			var target = new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_FirstAccessToCleanupInfo_NoNeedToCleanup()
		{
			MockRepository repo = new MockRepository();

			var cleanupInfo = new MemoryStream(); // cleanup.info doesn't exists - represented by empty stream

			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();
			Expect.Call(impl.OpenFile("cleanup.info", false)).Return(cleanupInfo).Repeat.Once();
			Expect.Call(env.Now).Return(new DateTime(2012, 1, 1, 02, 02, 02));
			Expect.Call(env.MinimumTimeBetweenCleanups).Return(TimeSpan.FromDays(1));
			Expect.Call(env.StartCleanupWorker(Arg<Action>.Is.Anything)).Repeat.Once().Return(new Task(() => { }));
			repo.ReplayAll();
			var target = new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
			repo.VerifyAll();
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TooEarlyToCleanup()
		{
			MockRepository repo = new MockRepository();

			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();
			Expect.Call(impl.OpenFile("cleanup.info", false)).Return(cleanupInfo).Repeat.Once();
			Expect.Call(env.Now).Return(new DateTime(2012, 1, 1, 02, 02, 02));
			Expect.Call(env.MinimumTimeBetweenCleanups).Return(TimeSpan.FromDays(1));
			DoNotExpect.Call(env.StartCleanupWorker(Arg<Action>.Is.Anything));
			repo.ReplayAll();
			var target = new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
			repo.VerifyAll();
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_CleanupInfoIsDisposedInCaseOfException()
		{
			MockRepository repo = new MockRepository();

			var cleanupInfo = new LogJoint.DelegatingStream(new MemoryStream());

			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();
			Expect.Call(impl.OpenFile("cleanup.info", false)).Return(cleanupInfo).Repeat.Once();
			Expect.Call(env.Now).Throw(new TestException());
			Expect.Call(env.MinimumTimeBetweenCleanups).Return(TimeSpan.FromDays(1));
			repo.ReplayAll();
			try
			{
				new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
			}
			catch (TestException)
			{
			}
			repo.VerifyAll();
			Assert.IsTrue(cleanupInfo.IsDisposed);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TimeToCleanup()
		{
			MockRepository repo = new MockRepository();

			byte[] cleanupInfoBuf = Encoding.ASCII.GetBytes("LC=2012/01/01 01:01:01");
			var cleanupInfo = new MemoryStream(cleanupInfoBuf, 0, cleanupInfoBuf.Length, true, true);

			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();
			Expect.Call(impl.OpenFile("cleanup.info", false)).Return(cleanupInfo).Repeat.Once();
			Expect.Call(env.Now).Return(new DateTime(2012, 2, 1, 02, 02, 02));
			Expect.Call(env.MinimumTimeBetweenCleanups).Return(TimeSpan.FromDays(1));
			Expect.Call(env.StartCleanupWorker(Arg<Action>.Is.NotNull)).Repeat.Once().Return(new Task(() => { }));
			repo.ReplayAll();
			var target = new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
			repo.VerifyAll();

			Assert.AreEqual("LC=2012/02/01 02:02:02", Encoding.ASCII.GetString(cleanupInfoBuf), "Current date must be written to cleanup.info");
		}

		void TestCleanupLogic(Action<StorageManager, MockRepository, IStorageImplementation, IEnvironment> logicTest)
		{
			MockRepository repo = new MockRepository();

			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");
			var impl = repo.CreateMock<IStorageImplementation>();
			var env = repo.CreateMock<IEnvironment>();

			Expect.Call(impl.OpenFile("cleanup.info", false)).Return(cleanupInfo).Repeat.Once();
			Expect.Call(env.Now).Return(new DateTime(2012, 1, 1, 11, 01, 01));
			Expect.Call(env.MinimumTimeBetweenCleanups).Return(TimeSpan.FromHours(6));
			Expect.Call(env.StartCleanupWorker(Arg<Action>.Is.NotNull)).Repeat.Once().Return(new Task(() => { }));
			repo.ReplayAll();
			var target = new StorageManager(env, impl, LogJoint.LJTraceSource.EmptyTracer);
			repo.VerifyAll();

			repo.BackToRecordAll();
			logicTest(target, repo, impl, env);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_NoNeedToCleanupBecauseOfSmallStorageSize()
		{
			TestCleanupLogic((target, repo, impl, env) =>
			{
				Expect.Call(impl.CalcStorageSize(Arg<CancellationToken>.Is.Anything)).Return((long)10000).Repeat.Once();
				Expect.Call(env.MaximumStorageSize).Return((long)10001);
				repo.ReplayAll();

				target.CleanupWorker();

				repo.VerifyAll();
			});
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_ActualCleanup()
		{
			TestCleanupLogic((target, repo, impl, env) =>
			{
				Expect.Call(impl.CalcStorageSize(Arg<CancellationToken>.Is.Anything)).Return((long)10000).Repeat.Once();
				Expect.Call(env.MaximumStorageSize).Return((long)5000);
				Expect.Call(impl.ListDirectories(Arg.Is<string>(""), Arg<CancellationToken>.Is.Anything)).Return(
					new string[] {"aa", "bb"}).Repeat.Once();
				var aaAccessTime = "LA=2011/12/22 01:01:01.002";
				var bbAccessTime = "LA=2011/12/22 01:01:01.001"; // bb is older
				Expect.Call(impl.OpenFile(@"aa\cleanup.info", true)).Repeat.Once().Return(CreateTextStream(aaAccessTime));
				Expect.Call(impl.OpenFile(@"bb\cleanup.info", true)).Repeat.Once().Return(CreateTextStream(bbAccessTime));
				Expect.Call(() => impl.DeleteDirectory("bb")).Repeat.Once(); // expect bb to be deleted
				repo.ReplayAll();

				target.CleanupWorker();

				repo.VerifyAll();
			});
		}
	}
}
