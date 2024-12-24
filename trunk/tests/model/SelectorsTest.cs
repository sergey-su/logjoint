using System;
using System.Linq;
using NSubstitute;
using LogJoint;
using System.Diagnostics;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SelectorsTest
	{
		[Test]
		public void IntArgTest()
		{
			int arg = 0;
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(0, Is.EqualTo(sel().Item2));

			arg = 1;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(1, Is.EqualTo(sel().Item2));
		}

		[Test]
		public void DateTimeArgTest()
		{
			DateTime arg = new DateTime(2000, 1, 3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i.Day));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));

			arg = arg.AddDays(1);
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(4, Is.EqualTo(sel().Item2));
		}

		[Test]
		public void StringArgTest()
		{
			string arg = "a";
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i ?? "-"));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That("a", Is.EqualTo(sel().Item2));

			arg = "b";
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That("b", Is.EqualTo(sel().Item2));

			arg = null;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That("-", Is.EqualTo(sel().Item2));
		}

		class MyRefType
		{
			public int x;
		};

		[Test]
		public void CustomReferenceTypeArgTest()
		{
			MyRefType arg = new MyRefType() { x = 2 };
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i != null ? i.x : -1));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(2, Is.EqualTo(sel().Item2));

			arg = new MyRefType() { x = 3 };
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));
			var r1 = sel();

			arg = new MyRefType() { x = 3 };
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(r1, Is.Not.SameAs(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));

			arg = null;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(-1, Is.EqualTo(sel().Item2));
		}

		[Test]
		public void NullableValueArgTest()
		{
			int? arg = 3;
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i != null ? i : -1));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));

			arg = 4;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(4, Is.EqualTo(sel().Item2));

			arg = null;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(-1, Is.EqualTo(sel().Item2));

			arg = 5;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(5, Is.EqualTo(sel().Item2));
		}

		[Test]
		public void TupleArgTest()
		{
			Tuple<int> arg = Tuple.Create(3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i != null ? i.Item1 : -1));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));
			var r1 = sel();

			arg = Tuple.Create(3);
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(r1, Is.EqualTo(sel()));
			Assert.That(3, Is.EqualTo(sel().Item2));

			arg = Tuple.Create(4);
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(4, Is.EqualTo(sel().Item2));

			arg = null;
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(-1, Is.EqualTo(sel().Item2));
		}

		struct MyStruct
		{
			public int x;
			public int y;
		};

		[Test]
		public void CustomValueTypeArgTest()
		{
			MyStruct arg = new MyStruct() { x = 2, y = 3 };
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i.x + i.y));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(5, Is.EqualTo(sel().Item2));
			var r1 = sel();

			arg = new MyStruct() { x = 2, y = 3 };
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(r1, Is.EqualTo(sel()));
			Assert.That(5, Is.EqualTo(sel().Item2));

			arg = new MyStruct() { x = 2, y = 4 };
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(6, Is.EqualTo(sel().Item2));
		}

		[Test]
		public void ValueTuplesArgTest()
		{
			var arg = (2, 3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i.Item1 + i.Item2));
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(5, Is.EqualTo(sel().Item2));
			var r1 = sel();

			arg = (2, 3);
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(r1, Is.EqualTo(sel()));
			Assert.That(5, Is.EqualTo(sel().Item2));

			arg = (2, 4);
			Assert.That(sel(), Is.EqualTo(sel()));
			Assert.That(6, Is.EqualTo(sel().Item2));
		}

	}
}
