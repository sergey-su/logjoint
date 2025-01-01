using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class LRUCacheTests
    {

        [Test]
        public void Tests()
        {
            var cache = new LRUCache<int, int>(3);

            Assert.That(0, Is.EqualTo(cache.Count));
            cache.Set(1, 10);
            Assert.That(1, Is.EqualTo(cache.Count));
            cache.Set(2, 20);
            Assert.That(2, Is.EqualTo(cache.Count));
            cache.Set(3, 30);
            Assert.That(3, Is.EqualTo(cache.Count));
            Assert.That(10, Is.EqualTo(cache[1]));
            Assert.That(20, Is.EqualTo(cache[2]));
            Assert.That(30, Is.EqualTo(cache[3]));

            cache.Set(4, 40);
            Assert.That(cache.ContainsKey(1), Is.False);
            Assert.That(40, Is.EqualTo(cache[4]));
            Assert.That(3, Is.EqualTo(cache.Count));

            cache[4] = 400;
            Assert.That(400, Is.EqualTo(cache[4]));

            cache[2] = 200;
            Assert.That(200, Is.EqualTo(cache[2]));
            cache[3] = 300;
            Assert.That(300, Is.EqualTo(cache[3]));

            cache.Set(1, 10);
            Assert.That(cache.ContainsKey(4), Is.False);

            cache.Clear();
            Assert.That(0, Is.EqualTo(cache.Count));
            Assert.That(cache.ContainsKey(1), Is.False);
        }
    }
}
