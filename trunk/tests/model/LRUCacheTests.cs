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

			Assert.AreEqual(0, cache.Count);
			cache.Set(1, 10);
			Assert.AreEqual(1, cache.Count);
			cache.Set(2, 20);
			Assert.AreEqual(2, cache.Count);
			cache.Set(3, 30);
			Assert.AreEqual(3, cache.Count);
			Assert.AreEqual(10, cache[1]);
			Assert.AreEqual(20, cache[2]);
			Assert.AreEqual(30, cache[3]);

			cache.Set(4, 40);
			Assert.IsFalse(cache.ContainsKey(1));
			Assert.AreEqual(40, cache[4]);
			Assert.AreEqual(3, cache.Count);

			cache[4] = 400;
			Assert.AreEqual(400, cache[4]);

			cache[2] = 200;
			Assert.AreEqual(200, cache[2]);
			cache[3] = 300;
			Assert.AreEqual(300, cache[3]);

			cache.Set(1, 10);
			Assert.IsFalse(cache.ContainsKey(4));

			cache.Clear();
			Assert.AreEqual(0, cache.Count);
			Assert.IsFalse(cache.ContainsKey(1));
		}
	}
}
