using LogJoint.Persistence.Implementation;
using LogJoint.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;

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

		IFileSystemAccess fsMock;
		ITimingAndThreading timingThreadingMock;
		IStorageConfigAccess settingsMock;
		IStorageManagerImplementation storageManager;

		[TestInitialize]
		public void Init()
		{
			fsMock = Substitute.For<IFileSystemAccess>();
			timingThreadingMock = Substitute.For<ITimingAndThreading>();
			settingsMock = Substitute.For<IStorageConfigAccess>();
		}

		void CreateSUT()
		{
			storageManager = new StorageManagerImplementation();
			storageManager.Init(timingThreadingMock, fsMock, settingsMock);
		}

		[TestMethod()]
		[ExpectedException(typeof(TestException))]
		public void StorageManager_AutoCleanup_StorageInfoAccessFailureFailsTheConstructor()
		{
			fsMock.OpenFile(null, false).ReturnsForAnyArgs(_ => { throw new TestException(); });
			CreateSUT();
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_FirstAccessToCleanupInfo_NoNeedToCleanup()
		{
			var cleanupInfo = new MemoryStream(); // cleanup.info doesn't exists - represented by empty stream

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(new DateTime(2012, 1, 1, 02, 02, 02));
			timingThreadingMock.StartTask(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			timingThreadingMock.ReceivedWithAnyArgs().StartTask(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TooEarlyToCleanup_LessThanMininumPeriod()
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(new DateTime(2012, 1, 1, 02, 02, 02)); // a bit more than an hour is less than mininum cleanup period

			CreateSUT();

			timingThreadingMock.DidNotReceiveWithAnyArgs().StartTask(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TooEarlyToCleanup()
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(new DateTime(2012, 1, 1, 16, 02, 02));
			settingsMock.CleanupPeriod.Returns(24);

			CreateSUT();

			timingThreadingMock.DidNotReceiveWithAnyArgs().StartTask(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_CleanupInfoIsDisposedInCaseOfException()
		{
			var cleanupInfo = new LogJoint.DelegatingStream(new MemoryStream());

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(_ => { throw new TestException(); });

			try
			{
				CreateSUT();
			}
			catch (TestException)
			{
			}

			Assert.IsTrue(cleanupInfo.IsDisposed);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TimeToCleanup()
		{
			byte[] cleanupInfoBuf = Encoding.ASCII.GetBytes("LC=2012/01/01 01:01:01");
			var cleanupInfo = new MemoryStream(cleanupInfoBuf, 0, cleanupInfoBuf.Length, true, true);

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(new DateTime(2012, 2, 1, 02, 02, 02));
			settingsMock.CleanupPeriod.Returns(24);
			timingThreadingMock.StartTask(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			Assert.AreEqual("LC=2012/02/01 02:02:02", Encoding.ASCII.GetString(cleanupInfoBuf), "Current date must be written to cleanup.info");
			timingThreadingMock.ReceivedWithAnyArgs().StartTask(null);
		}

		void TestCleanupLogic(Action<StorageManagerImplementation> logicTest)
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			fsMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			timingThreadingMock.Now.Returns(new DateTime(2012, 1, 1, 11, 01, 01));
			timingThreadingMock.StartTask(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			logicTest((StorageManagerImplementation)storageManager);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_NoNeedToCleanupBecauseOfSmallStorageSize()
		{
			TestCleanupLogic((target) =>
			{
				fsMock.CalcStorageSize(CancellationToken.None).ReturnsForAnyArgs((long)StorageSizes.MinStoreSizeLimit * 2);
				settingsMock.SizeLimit.Returns(StorageSizes.MinStoreSizeLimit * 3);

				target.CleanupWorker();

				fsMock.DidNotReceiveWithAnyArgs().ListDirectories(null, CancellationToken.None);
			});
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_ActualCleanup()
		{
			TestCleanupLogic((target) =>
			{
				fsMock.CalcStorageSize(CancellationToken.None).ReturnsForAnyArgs((long)1024*1024*500);
				fsMock.ListDirectories("", Arg.Any<CancellationToken>()).Returns(
					new string[] {"aa", "bb"});
				var aaAccessTime = "LA=2011/12/22 01:01:01.002";
				var bbAccessTime = "LA=2011/12/22 01:01:01.001"; // bb is older
				fsMock.OpenFile(@"aa\cleanup.info", true).Returns(CreateTextStream(aaAccessTime));
				fsMock.OpenFile(@"bb\cleanup.info", true).Returns(CreateTextStream(bbAccessTime));

				target.CleanupWorker();

				fsMock.Received(1).DeleteDirectory("bb"); // expect bb to be deleted
				fsMock.DidNotReceive().DeleteDirectory("aa");
			});
		}
	}
}
