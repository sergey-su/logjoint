using NUnit.Framework;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	[TestFixture]
	public class EnumerableAsyncTests
	{
		[Test]
		public async Task EnumerableAsyncProduceTest()
		{
			int sequenceLen = 10000;
			var producer = EnumerableAsync.Produce<int>(async yielder =>
			{
				for (var i = 0; i < sequenceLen; ++i)
				{
					await yielder.YieldAsync(i);
					await Task.Yield();
				}
			});
			var multiplexed = producer.Multiplex();
			var list1 = multiplexed.Select(async i =>
			{
				await Task.Yield();
				return i * 10;
			}).ToList();
			var list2 = multiplexed.Select(async i =>
			{
				await Task.Yield();
				return i * 100;
			}).ToList();
			await Task.WhenAll(multiplexed.Open(), list1, list2);
			for (int i = 0; i < sequenceLen; ++i)
			{
				Assert.That(i * 10, Is.EqualTo(list1.Result[i]));
				Assert.That(i * 100, Is.EqualTo(list2.Result[i]));
			}
		}
	}
}