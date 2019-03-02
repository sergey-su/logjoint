using System;
using System.Linq;
using NSubstitute;
using LogJoint;
using System.Diagnostics;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class BookmarksTest
	{
		readonly static long testTSBase = 10000000;
		readonly IChangeNotification changeNotification  = Substitute.For<IChangeNotification>();

		[DebuggerStepThrough]
		static MessageTimestamp ToTestTS(int time)
		{
			return new MessageTimestamp(new DateTime(testTSBase + time * 1000, DateTimeKind.Utc));
		}

		[DebuggerStepThrough]
		static int FromTestTS(MessageTimestamp time)
		{
			return (int)((time.ToUniversalTime().Ticks - testTSBase)/1000);
		}

		[DebuggerStepThrough]
		static IMessage CreateMsg(int time, string logSourceConnectionId, long position, bool isBookmarked = false)
		{
			var ret = Substitute.For<IMessage>();
			ret.Time.Returns(ToTestTS(time));
			ret.Position.Returns(position);
			ret.LogSource.ConnectionId.Returns(logSourceConnectionId);
			return ret;
		}

		[DebuggerStepThrough]
		static IBookmark CreateBmk(int time, string logSourceConnectionId, long position)
		{
			var ret = Substitute.For<IBookmark>();
			ret.Time.Returns(ToTestTS(time));
			ret.LogSourceConnectionId.Returns(logSourceConnectionId);
			ret.Position.Returns(position);
			return ret;
		}

		[DebuggerStepThrough]
		static IBookmarksFactory CreateBmkFactory()
		{
			var ret = Substitute.For<IBookmarksFactory>();
			ret.CreateBookmark((IMessage)null, 0).ReturnsForAnyArgs(callInfo => 
			{
				var msg = (IMessage)(callInfo.Args()[0]);
				return CreateBmk(FromTestTS(msg.Time), msg.LogSource.ConnectionId, msg.Position);
			});
			return ret;
		}

		[Test]
		public void SimpleNextSearchTest()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory(), changeNotification);
			var b1 = bmks.ToggleBookmark(CreateBmk(10, "", 0));
			var b2 = bmks.ToggleBookmark(CreateBmk(20, "", 0));

			var foundBmk = bmks.GetNext(CreateBmk(0, "", 0), true);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(30, "", 0), false);
			Assert.AreSame(b2, foundBmk);
		}

		[Test]
		public void UnseccessfulNextSearchTest()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory(), changeNotification);
			bmks.ToggleBookmark(CreateBmk(10, "", 0));
			bmks.ToggleBookmark(CreateBmk(20, "", 0));

			var foundBmk = bmks.GetNext(CreateBmk(40, "", 0), true);
			Assert.IsNull(foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(5, "", 0), false);
			Assert.IsNull(foundBmk);
		}

		[Test]
		public void UnseccessfulNextSearchInEmptyBookmarksContainer()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory(), changeNotification);

			var foundBmk = bmks.GetNext(CreateBmk(0, "", 0), true);
			Assert.IsNull(foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(0, "", 0), false);
			Assert.IsNull(foundBmk);
		}

		[Test]
		public void SearchByTimestampThatHasManyBookmars()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory(), changeNotification);
			var b0 = bmks.ToggleBookmark(CreateBmk(0, "", 10));
			var b1 = bmks.ToggleBookmark(CreateBmk(10, "", 300));
			var b2 = bmks.ToggleBookmark(CreateBmk(10, "", 100));
			var b3 = bmks.ToggleBookmark(CreateBmk(10, "", 200));
			var b4 = bmks.ToggleBookmark(CreateBmk(20, "", 1000));

			IBookmark foundBmk;

			foundBmk = bmks.GetNext(CreateBmk(5, "", 0), true);
			Assert.AreSame(b2, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 321), true);
			Assert.AreSame(b4, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 200), true);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 234), true);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 179), true);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 300), true);
			Assert.AreSame(b4, foundBmk);


			foundBmk = bmks.GetNext(CreateBmk(11, "", 0), false);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 87), false);
			Assert.AreSame(b0, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 300), false);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 100), false);
			Assert.AreSame(b0, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 234), false);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateBmk(10, "", 200), false);
			Assert.AreSame(b2, foundBmk);
		}
	}
}
