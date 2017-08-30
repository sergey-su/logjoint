using System;
using System.Text;
using System.Collections.Generic;
using Rhino.Mocks;
using System.IO;
using LogJoint.LogMedia;
using LogJoint;
using NUnit.Framework;

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
		public void ConstructorAndUpdate()
		{
			MockRepository rep = new MockRepository();

			DateTime modifTime = new DateTime(2000, 1, 1);
			long size = 100;
			MyFileStream stm = rep.CreateMock<MyFileStream>(rep);

			Expect.Call(() => stm.Dispose()).Repeat.AtLeastOnce();

			Expect.Call(stm.Length).Return(size);
			Expect.Call(stm.IsDeleted).Repeat.Any().Return(false);
			Expect.Call(stm.LastWriteTime).Repeat.Any().Return(modifTime);

			IFileSystem fs = rep.CreateMock<IFileSystem>();
			Expect.Call(fs.OpenFile("test")).Return(stm);

			rep.ReplayAll();

			using (SimpleFileMedia media = new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				Assert.AreEqual(modifTime, media.LastModified);
				Assert.AreEqual(size, media.Size);
			}

			rep.VerifyAll();
		}

		class TestException: Exception
		{
		};

		[Test]
		public void ExceptionInConstructorMustNotLeakStreams()
		{
			MockRepository rep = new MockRepository();

			MyFileStream stm = rep.CreateMock<MyFileStream>(rep);
			stm.Dispose();
			LastCall.On(stm).Repeat.AtLeastOnce();
			Exception ex = new TestException();
			Expect.Call(stm.Length).Repeat.Times(0, 1).Throw(ex);
			Expect.Call(stm.IsDeleted).Repeat.Times(0, 1).Throw(ex);
			Expect.Call(stm.LastWriteTime).Repeat.Times(0, 1).Throw(ex);

			IFileSystem fs = rep.CreateMock<IFileSystem>();
			Expect.Call(fs.OpenFile("test")).Return(stm);

			rep.ReplayAll();

			try
			{
				(new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test"))).Dispose();
			}
			catch (TestException)
			{
			}

			rep.VerifyAll();
		}

		[Test]
		public void UpdatingWhileFileIsGrowing()
		{
			MockRepository rep = new MockRepository();
			IFileSystem fs = rep.CreateMock<IFileSystem>();
			MyFileStream stm = rep.CreateMock<MyFileStream>(rep);

			Expect.Call(fs.OpenFile("test")).Return(stm);

			DateTime time1 = new DateTime(2000, 1, 1);
			long size1 = 100;
			Expect.Call(stm.Length).Repeat.Any().Return(size1);
			Expect.Call(stm.LastWriteTime).Repeat.Any().Return(time1);
			Expect.Call(stm.IsDeleted).Repeat.Any().Return(false);

			rep.ReplayAll();

			using (SimpleFileMedia media = new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);
				Assert.AreEqual(size1, media.DataStream.Length);

				rep.VerifyAll();

				rep.BackToRecordAll();

				DateTime time2 = new DateTime(2000, 2, 2);
				long size2 = 200;
				Expect.Call(stm.Length).Repeat.Any().Return(size2);
				Expect.Call(stm.LastWriteTime).Repeat.Any().Return(time2);
				Expect.Call(stm.IsDeleted).Repeat.Any().Return(false);

				rep.ReplayAll();

				media.Update();

				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);

				rep.VerifyAll();
				
				rep.BackToRecordAll();
				stm.Dispose();
				LastCall.On(stm).Repeat.AtLeastOnce();
				rep.ReplayAll();
			}

			rep.VerifyAll();
		}

		[Test]
		public void FileDeletedByAnotherProcessAndThenNewFileAppeared()
		{
			MockRepository rep = new MockRepository();

			IFileSystem fs = rep.CreateMock<IFileSystem>();

			// Create and init the first stream
			long initialSize1 = 100;
			DateTime modifTime1 = new DateTime(2000, 3, 4);
			MyFileStream stm1 = rep.CreateMock<MyFileStream>(rep);
			Expect.Call(stm1.Length).Repeat.Any().Return(initialSize1);
			Expect.Call(stm1.IsDeleted).Repeat.Any().Return(false);
			Expect.Call(stm1.LastWriteTime).Repeat.Any().Return(modifTime1);

			// Instruct file system to return the first stream
			Expect.Call(fs.OpenFile("test")).Return(stm1);

			rep.ReplayAll();

			using (SimpleFileMedia media = new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				// Check that media refers to the first stream stm1
				Assert.AreEqual(initialSize1, media.DataStream.Length);
				Assert.AreEqual(initialSize1, media.Size);
				Assert.AreEqual(true, media.IsAvailable);

				rep.VerifyAll();



				rep.BackToRecordAll();
				// Simulate file deletion: Length and LastWriteTime keep returning file properties,
				// but IsDeleted now returns "true".
				Expect.Call(stm1.Length).Repeat.Any().Return(initialSize1);
				Expect.Call(stm1.LastWriteTime).Repeat.Any().Return(modifTime1);
				Expect.Call(stm1.IsDeleted).Repeat.Any().Return(true);

				// We expect stream stm1 to be released/disposed
				stm1.Dispose();
				LastCall.On(stm1).Repeat.AtLeastOnce();

				// Factory cannot open the file that has been deleted while being locked
				Expect.Call(fs.OpenFile("test")).Repeat.Any().Throw(new UnauthorizedAccessException());
				rep.ReplayAll();


				// Properties must return previous values as long as Update is not called
				Assert.AreEqual(initialSize1, media.Size);
				Assert.AreEqual(initialSize1, media.DataStream.Length);
				Assert.AreEqual(true, media.IsAvailable);

				// This update should detect file deletion and release it
				media.Update();
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);

				// Subsequent Updates should change nothing
				media.Update();
				media.Update();
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);

				rep.VerifyAll();


				rep.BackToRecordAll();
				// Simulate that new file with name "test" appeared 
				long initialSize2 = 200;
				DateTime modifTime2 = new DateTime(2000, 4, 5);
				MyFileStream stm2 = rep.CreateMock<MyFileStream>(rep);
				Expect.Call(stm2.Length).Repeat.Any().Return(initialSize2);
				Expect.Call(stm2.IsDeleted).Repeat.Any().Return(false);
				Expect.Call(stm2.LastWriteTime).Repeat.Any().Return(modifTime2);
				stm2.Dispose();
				LastCall.On(stm2).Repeat.AtLeastOnce();
				Expect.Call(fs.OpenFile("test")).Return(stm2);
				rep.ReplayAll();


				// Properties must return previous values as long as Update is not called
				Assert.AreEqual(0, media.Size);
				Assert.AreEqual(0, media.DataStream.Length);
				Assert.AreEqual(false, media.IsAvailable);

				// This Update will pick up new file
				media.Update();
				Assert.AreEqual(initialSize2, media.DataStream.Length);
				Assert.AreEqual(initialSize2, media.Size);
				Assert.AreEqual(true, media.IsAvailable);

				// Subsequent Updates should change nothing
				media.Update();
				media.Update();
				Assert.AreEqual(initialSize2, media.Size);
				Assert.AreEqual(initialSize2, media.DataStream.Length);
				Assert.AreEqual(true, media.IsAvailable);
			}

			rep.VerifyAll();
		}

		[Test]
		public void MediaPropertiesMustChangeOnlyAfterUpdate()
		{
			MockRepository rep = new MockRepository();
			IFileSystem fs = rep.CreateMock<IFileSystem>();
			MyFileStream stm = rep.CreateMock<MyFileStream>(rep);

			Expect.Call(fs.OpenFile("test")).Return(stm);

			DateTime time1 = new DateTime(2000, 1, 1);
			long size1 = 100;
			Expect.Call(stm.Length).Repeat.Any().Return(size1);
			Expect.Call(stm.LastWriteTime).Repeat.Any().Return(time1);
			Expect.Call(stm.IsDeleted).Repeat.Any().Return(false);

			rep.ReplayAll();

			using (SimpleFileMedia media = new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test")))
			{
				// Media properties are the same as stm's ones
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);

				rep.VerifyAll();

				rep.BackToRecordAll();
				// Chnage the properties of stm
				DateTime time2 = new DateTime(2000, 2, 2);
				long size2 = 200;
				Expect.Call(stm.Length).Repeat.Any().Return(size2);
				Expect.Call(stm.LastWriteTime).Repeat.Any().Return(time2);
				Expect.Call(stm.IsDeleted).Repeat.Any().Return(false);
				rep.ReplayAll();

				// Properties have not still changed
				Assert.AreEqual(time1, media.LastModified);
				Assert.AreEqual(size1, media.Size);

				// This Update should refresh media's properties
				media.Update();

				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);

				// Subsequent calls change nothing
				media.Update();
				media.Update();
				Assert.AreEqual(time2, media.LastModified);
				Assert.AreEqual(size2, media.Size);

				rep.VerifyAll();


				rep.BackToRecordAll();
				stm.Dispose();
				LastCall.On(stm).Repeat.AtLeastOnce();
				rep.ReplayAll();
			}

			rep.VerifyAll();
		}

		[Test] 
		public void InitialUpdateWithInvalidFileNameMustResultInException()
		{
			Assert.Throws<FileNotFoundException>(()=>
			{
				MockRepository rep = new MockRepository();
				IFileSystem fs = rep.CreateMock<IFileSystem>();

				Expect.Call(fs.OpenFile("test")).Throw(new FileNotFoundException());

				rep.ReplayAll();

				SimpleFileMedia media = new SimpleFileMedia(fs, SimpleFileMedia.CreateConnectionParamsFromFileName("test"));
			
				rep.VerifyAll();
			});
		}

	}


}
