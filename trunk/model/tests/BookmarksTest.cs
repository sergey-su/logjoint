using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using LogJoint;
using System.Diagnostics;

namespace LogJointTests
{
	[TestClass]
	public class BookmarksTest
	{
		readonly static long testTSBase = 10000000;

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
		static IMessage CreateMsg(int time, string logSourceConnectionId, int hash, bool isBookmarked = false)
		{
			var ret = Substitute.For<IMessage>();
			ret.Time.Returns(ToTestTS(time));
			ret.GetHashCode().Returns(hash);
			ret.LogSource.ConnectionId.Returns(logSourceConnectionId);
			return ret;
		}

		[DebuggerStepThrough]
		static IBookmark CreateBmk(int time, string logSourceConnectionId, int hash)
		{
			var ret = Substitute.For<IBookmark>();
			ret.Time.Returns(ToTestTS(time));
			ret.LogSourceConnectionId.Returns(logSourceConnectionId);
			ret.Position.Returns(new long?());
			ret.MessageHash.Returns(hash);
			return ret;
		}

		[DebuggerStepThrough]
		static IBookmarksFactory CreateBmkFactory()
		{
			var ret = Substitute.For<IBookmarksFactory>();
			ret.CreateBookmark((IMessage)null).ReturnsForAnyArgs(callInfo => 
			{
				var msg = (IMessage)(callInfo.Args()[0]);
				return CreateBmk(FromTestTS(msg.Time), msg.LogSource.ConnectionId, msg.GetHashCode());
			});
			return ret;
		}

		[TestMethod]
		public void SimpleNextSearchTest()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory());
			var b1 = bmks.ToggleBookmark(CreateBmk(10, "", 0));
			var b2 = bmks.ToggleBookmark(CreateBmk(20, "", 0));

			var foundBmk = bmks.GetNext(CreateMsg(0, "", 0), true, Substitute.For<INextBookmarkCallback>());
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(30, "", 0), false, Substitute.For<INextBookmarkCallback>());
			Assert.AreSame(b2, foundBmk);
		}

		[TestMethod]
		public void UnseccessfulNextSearchTest()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory());
			bmks.ToggleBookmark(CreateBmk(10, "", 0));
			bmks.ToggleBookmark(CreateBmk(20, "", 0));

			var foundBmk = bmks.GetNext(CreateMsg(40, "", 0), true, Substitute.For<INextBookmarkCallback>());
			Assert.IsNull(foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(5, "", 0), false, Substitute.For<INextBookmarkCallback>());
			Assert.IsNull(foundBmk);
		}

		[TestMethod]
		public void UnseccessfulNextSearchInEmptyBookmarksContainer()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory());

			var foundBmk = bmks.GetNext(CreateMsg(0, "", 0), true, Substitute.For<INextBookmarkCallback>());
			Assert.IsNull(foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(0, "", 0), false, Substitute.For<INextBookmarkCallback>());
			Assert.IsNull(foundBmk);
		}

		[TestMethod]
		public void SearchByTimestampThatHasManyBookmars()
		{
			IBookmarks bmks = new Bookmarks(CreateBmkFactory());
			var b0 = bmks.ToggleBookmark(CreateBmk(0, "", 10));
			var b1 = bmks.ToggleBookmark(CreateBmk(10, "", 300));
			var b2 = bmks.ToggleBookmark(CreateBmk(10, "", 100));
			var b3 = bmks.ToggleBookmark(CreateBmk(10, "", 200));
			var b4 = bmks.ToggleBookmark(CreateBmk(20, "", 1000));

			var messagesAt10 = new IMessage[]
			{
				CreateMsg(10, "", 246),
				CreateMsg(10, "", 321),
				CreateMsg(10, "", 135),
				CreateMsg(10, "", 200, isBookmarked: true), // b3
				CreateMsg(10, "", 234),
				CreateMsg(10, "", 100, isBookmarked: true), // b2
				CreateMsg(10, "", 179),
				CreateMsg(10, "", 300, isBookmarked: true), // b1
				CreateMsg(10, "", 87),
			};
			var cb = Substitute.For<INextBookmarkCallback>();
			cb.EnumMessages(ToTestTS(10), true).Returns(messagesAt10);
			cb.EnumMessages(ToTestTS(10), false).Returns(messagesAt10.Reverse().ToArray());

			IBookmark foundBmk;


			foundBmk = bmks.GetNext(CreateMsg(5, "", 0), true, cb);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 321), true, cb);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 200), true, cb);
			Assert.AreSame(b2, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 234), true, cb);
			Assert.AreSame(b2, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 179), true, cb);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 300), true, cb);
			Assert.AreSame(b4, foundBmk);


			foundBmk = bmks.GetNext(CreateMsg(11, "", 0), false, cb);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 87), false, cb);
			Assert.AreSame(b1, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 300), false, cb);
			Assert.AreSame(b2, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 100), false, cb);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 234), false, cb);
			Assert.AreSame(b3, foundBmk);

			foundBmk = bmks.GetNext(CreateMsg(10, "", 200), false, cb);
			Assert.AreSame(b0, foundBmk);
		}
	}
}
