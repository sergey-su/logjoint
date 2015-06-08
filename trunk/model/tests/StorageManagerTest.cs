using LogJoint.Persistence;
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

		IStorageImplementation implMock;
		IEnvironment envMock;
		IGlobalSettingsAccessor settingsMock;
		IStorageManager storageManager;

		[TestInitialize]
		public void Init()
		{
			implMock = Substitute.For<IStorageImplementation>();
			envMock = Substitute.For<IEnvironment>();
			settingsMock = Substitute.For<IGlobalSettingsAccessor>();

			envMock.CreateSettingsAccessor(null).ReturnsForAnyArgs(settingsMock);
		}

		void CreateSUT()
		{
			storageManager = new StorageManager(envMock, implMock);
		}

		[TestMethod()]
		[ExpectedException(typeof(TestException))]
		public void StorageManager_AutoCleanup_StorageInfoAccessFailureFailsTheConstructor()
		{
			implMock.OpenFile(null, false).ReturnsForAnyArgs(_ => { throw new TestException(); });
			CreateSUT();
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_FirstAccessToCleanupInfo_NoNeedToCleanup()
		{
			var cleanupInfo = new MemoryStream(); // cleanup.info doesn't exists - represented by empty stream

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(new DateTime(2012, 1, 1, 02, 02, 02));
			envMock.StartCleanupWorker(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			envMock.ReceivedWithAnyArgs().StartCleanupWorker(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TooEarlyToCleanup_LessThanMininumPeriod()
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(new DateTime(2012, 1, 1, 02, 02, 02)); // a bit more than an hour is less than mininum cleanup period

			CreateSUT();

			envMock.DidNotReceiveWithAnyArgs().StartCleanupWorker(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_TooEarlyToCleanup()
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(new DateTime(2012, 1, 1, 16, 02, 02));
			settingsMock.StorageSizes.Returns(new StorageSizes() { CleanupPeriod = 24 });

			CreateSUT();

			envMock.DidNotReceiveWithAnyArgs().StartCleanupWorker(null);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanup_CleanupInfoIsDisposedInCaseOfException()
		{
			var cleanupInfo = new LogJoint.DelegatingStream(new MemoryStream());

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(_ => { throw new TestException(); });

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

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(new DateTime(2012, 2, 1, 02, 02, 02));
			settingsMock.StorageSizes.Returns(new StorageSizes() { CleanupPeriod = 24 });
			envMock.StartCleanupWorker(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			Assert.AreEqual("LC=2012/02/01 02:02:02", Encoding.ASCII.GetString(cleanupInfoBuf), "Current date must be written to cleanup.info");
			envMock.ReceivedWithAnyArgs().StartCleanupWorker(null);
		}

		void TestCleanupLogic(Action<StorageManager> logicTest)
		{
			var cleanupInfo = CreateTextStream("LC=2012/01/01 01:01:01");

			implMock.OpenFile("cleanup.info", false).Returns(cleanupInfo);
			envMock.Now.Returns(new DateTime(2012, 1, 1, 11, 01, 01));
			envMock.StartCleanupWorker(null).ReturnsForAnyArgs(Task.FromResult(0));

			CreateSUT();

			logicTest((StorageManager)storageManager);
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_NoNeedToCleanupBecauseOfSmallStorageSize()
		{
			TestCleanupLogic((target) =>
			{
				implMock.CalcStorageSize(CancellationToken.None).ReturnsForAnyArgs((long)StorageSizes.MinStoreSizeLimit * 2);
				settingsMock.StorageSizes.Returns(new StorageSizes() { StoreSizeLimit = StorageSizes.MinStoreSizeLimit * 3 });

				target.CleanupWorker();

				implMock.DidNotReceiveWithAnyArgs().ListDirectories(null, CancellationToken.None);
			});
		}

		[TestMethod()]
		public void StorageManager_AutoCleanupLogic_ActualCleanup()
		{
			TestCleanupLogic((target) =>
			{
				implMock.CalcStorageSize(CancellationToken.None).ReturnsForAnyArgs((long)1024*1024*500);
				implMock.ListDirectories("", Arg.Any<CancellationToken>()).Returns(
					new string[] {"aa", "bb"});
				var aaAccessTime = "LA=2011/12/22 01:01:01.002";
				var bbAccessTime = "LA=2011/12/22 01:01:01.001"; // bb is older
				implMock.OpenFile(@"aa\cleanup.info", true).Returns(CreateTextStream(aaAccessTime));
				implMock.OpenFile(@"bb\cleanup.info", true).Returns(CreateTextStream(bbAccessTime));

				target.CleanupWorker();

				implMock.Received(1).DeleteDirectory("bb"); // expect bb to be deleted
				implMock.DidNotReceive().DeleteDirectory("aa");
			});
		}
	}
}
