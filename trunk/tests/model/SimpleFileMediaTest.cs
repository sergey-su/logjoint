using System;
using NSubstitute;
using System.IO;
using LogJoint.LogMedia;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SimpleFileMediaTest
	{
		public abstract class MyFileStream : Stream, IFileStreamInfo
		{
			public MyFileStream(object workaround) {}

			public abstract DateTime LastWriteTime { get; } 
			public abstract bool IsDeleted { get; }
		};

		[Test]
		public async Task ConstructorAndUpdate()
		{
			DateTime modifTime = new DateTime(2000, 1, 1);
			long size = 100;
			MyFileStream stm = Substitute.For<MyFileStream>(new object());

			stm.Length.Returns(size);
			stm.IsDeleted.Returns(false);
			stm.LastWriteTime.Returns(modifTime);

			IFileSystem fs = Substitute.For<IFileSystem>();
			fs.OpenFile("test").Returns(stm);

			using (SimpleFileMedia media = await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				Assert.AreEqual(modifTime, media.LastModified);
				Assert.AreEqual(size, media.Size);
			}

			stm.Received(1).Dispose();
		}

		class TestException: Exception
		{
		};

		[Test]
		public async Task ExceptionInConstructorMustNotLeakStreams()
		{
			MyFileStream stm = Substitute.For<MyFileStream>(new object());
			Exception ex = new TestException();
			stm.Length.Returns(callInfo => { throw ex; });
			stm.IsDeleted.Returns(callInfo => { throw ex; });
			stm.LastWriteTime.Returns(callInfo => { throw ex; });

			IFileSystem fs = Substitute.For<IFileSystem>();
			fs.OpenFile("test").Returns(stm);

			try
			{
				(await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test"))).Dispose();
			}
			catch (TestException)
			{
			}

			stm.Received(1).Dispose();
		}

		[Test]
		public async Task UpdatingWhileFileIsGrowing()
		{
			IFileSystem fs = Substitute.For<IFileSystem>();
			MyFileStream stm = Substitute.For<MyFileStream>(new object());

			fs.OpenFile("test").Returns(stm);

			DateTime time1 = new DateTime(2000, 1, 1);
			long size1 = 100;
			stm.Length.Returns(size1);
			stm.LastWriteTime.Returns(time1);
			stm.IsDeleted.Returns(false);

			using (SimpleFileMedia media = await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);
				Assert.AreEqual(size1, media.DataStream.Length);

				DateTime time2 = new DateTime(2000, 2, 2);
				long size2 = 200;
				stm.Length.Returns(size2);
				stm.LastWriteTime.Returns(time2);
				stm.IsDeleted.Returns(false);

				await media.Update();

				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);
			}

			stm.Received(1).Dispose();
		}

		[Test]
		public async Task FileDeletedByAnotherProcessAndThenNewFileAppeared()
		{
			IFileSystem fs = Substitute.For<IFileSystem>();

			// Create and init the first stream
			long initialSize1 = 100;
			DateTime modifTime1 = new DateTime(2000, 3, 4);
			MyFileStream stm1 = Substitute.For<MyFileStream>(new object());
			stm1.Length.Returns(initialSize1);
			stm1.IsDeleted.Returns(false);
			stm1.LastWriteTime.Returns(modifTime1);

			// Instruct file system to return the first stream
			fs.OpenFile("test").Returns(stm1);

			MyFileStream stm2 = Substitute.For<MyFileStream>(new object());

			using (SimpleFileMedia media = await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				// Check that media refers to the first stream stm1
				Assert.AreEqual(initialSize1, media.DataStream.Length);
				Assert.AreEqual(initialSize1, media.Size);
				Assert.AreEqual(true, media.IsAvailable);

				
				// Simulate file deletion: Length and LastWriteTime keep returning file properties,
				// but IsDeleted now returns "true".
				stm1.IsDeleted.Returns(true);


				// Factory cannot open the file that has been deleted while being locked
				fs.OpenFile("test").Returns(
					_ => throw new UnauthorizedAccessException(),
					_ => throw new UnauthorizedAccessException(),
					_ => stm2
					);


				// Properties must return previous values as long as Update is not called
				Assert.AreEqual(initialSize1, media.Size);
				Assert.AreEqual(initialSize1, media.DataStream.Length);
				Assert.AreEqual(true, media.IsAvailable);

				// This update should detect file deletion and release it
				await media.Update();
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);
				stm1.Received(1).Dispose();

				// Subsequent Updates should change nothing
				await media.Update();
				await media.Update();
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);


				// Simulate that new file with name "test" appeared 
				long initialSize2 = 200;
				DateTime modifTime2 = new DateTime(2000, 4, 5);
				stm2.Length.Returns(initialSize2);
				stm2.IsDeleted.Returns(false);
				stm2.LastWriteTime.Returns(modifTime2);


				// Properties must return previous values as long as Update is not called
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);

				// This Update will pick up new file
				await media.Update();
				Assert.AreEqual(initialSize2, media.DataStream.Length);
				Assert.AreEqual(initialSize2, media.Size);
				Assert.AreEqual(true, media.IsAvailable);

				// Subsequent Updates should change nothing
				await media.Update();
				await media.Update();
				Assert.AreEqual(initialSize2, media.Size);
				Assert.AreEqual(initialSize2, media.DataStream.Length);
				Assert.AreEqual(true, media.IsAvailable);
			}

			stm2.Received(1).Dispose();
		}

		[Test]
		public async Task MediaPropertiesMustChangeOnlyAfterUpdate()
		{
			IFileSystem fs = Substitute.For<IFileSystem>();
			MyFileStream stm = Substitute.For<MyFileStream>(new object());

			fs.OpenFile("test").Returns(stm);

			DateTime time1 = new DateTime(2000, 1, 1);
			long size1 = 100;
			stm.Length.Returns(size1);
			stm.LastWriteTime.Returns(time1);
			stm.IsDeleted.Returns(false);

			using (SimpleFileMedia media = await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				// Media properties are the same as stm's ones
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);

				// Change the properties of stm
				DateTime time2 = new DateTime(2000, 2, 2);
				long size2 = 200;
				stm.Length.Returns(size2);
				stm.LastWriteTime.Returns(time2);
				stm.IsDeleted.Returns(false);

				// Properties have not still changed
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);

				// This Update should refresh media's properties
				await media.Update();

				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);

				// Subsequent calls change nothing
				await media.Update();
				await media.Update();
				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);
			}

			stm.Received(1).Dispose();
		}

		[Test] 
		public void InitialUpdateWithInvalidFileNameMustResultInException()
		{
			Assert.ThrowsAsync<FileNotFoundException>(async () =>
			{
				IFileSystem fs = Substitute.For<IFileSystem>();

				fs.OpenFile("test").Returns(_ => throw new FileNotFoundException());

				SimpleFileMedia media = await SimpleFileMedia.Create(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test"));
			});
		}
	}
}
