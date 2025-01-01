using System;
using System.Linq;
using LogJoint.UI.Presenters.LogViewer;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;

namespace LogJoint.UI.Presenters.Tests.ExtensionsTests
{
    [TestFixture]
    public class ExtensionsTests
    {
        IMessage message;

        [SetUp]
        public void Setup()
        {
            message = Substitute.For<IMessage>();
        }

        ViewLine VL(int textLineIndex, bool bookmarked)
        {
            return new ViewLine()
            {
                Message = message,
                TextLineIndex = textLineIndex,
                IsBookmarked = bookmarked
            };
        }

        [Test]
        public void Test1()
        {

        }
    }
}