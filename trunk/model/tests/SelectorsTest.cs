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
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(0, sel().Item2);

			arg = 1;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(1, sel().Item2);
		}

		[Test]
		public void DateTimeArgTest()
		{
			DateTime arg = new DateTime(2000, 1, 3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i.Day));
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(3, sel().Item2);

			arg = arg.AddDays(1);
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(4, sel().Item2);
		}

		[Test]
		public void StringArgTest()
		{
			string arg = "a";
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i ?? "-"));
			Assert.AreSame(sel(), sel());
			Assert.AreEqual("a", sel().Item2);

			arg = "b";
			Assert.AreSame(sel(), sel());
			Assert.AreEqual("b", sel().Item2);

			arg = null;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual("-", sel().Item2);
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
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(2, sel().Item2);

			arg = new MyRefType() { x = 3 };
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(3, sel().Item2);
			var r1 = sel();

			arg = new MyRefType() { x = 3 };
			Assert.AreSame(sel(), sel());
			Assert.AreNotSame(r1, sel());
			Assert.AreEqual(3, sel().Item2);

			arg = null;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(-1, sel().Item2);
		}

		[Test]
		public void NullableValueArgTest()
		{
			int? arg = 3;
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i != null ? i : -1));
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(3, sel().Item2);

			arg = 4;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(4, sel().Item2);

			arg = null;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(-1, sel().Item2);

			arg = 5;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(5, sel().Item2);
		}

		[Test]
		public void TupleArgTest()
		{
			Tuple<int> arg = Tuple.Create(3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i != null ? i.Item1 : -1));
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(3, sel().Item2);
			var r1 = sel();

			arg = Tuple.Create(3);
			Assert.AreSame(sel(), sel());
			Assert.AreSame(r1, sel());
			Assert.AreEqual(3, sel().Item2);

			arg = Tuple.Create(4);
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(4, sel().Item2);

			arg = null;
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(-1, sel().Item2);
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
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(5, sel().Item2);
			var r1 = sel();

			arg = new MyStruct() { x = 2, y = 3 };
			Assert.AreSame(sel(), sel());
			Assert.AreSame(r1, sel());
			Assert.AreEqual(5, sel().Item2);

			arg = new MyStruct() { x = 2, y = 4 };
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(6, sel().Item2);
		}

		[Test]
		public void ValueTuplesArgTest()
		{
			var arg = (2, 3);
			var sel = Selectors.Create(() => arg, i => Tuple.Create(10, i.Item1 + i.Item2));
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(5, sel().Item2);
			var r1 = sel();

			arg = (2, 3);
			Assert.AreSame(sel(), sel());
			Assert.AreSame(r1, sel());
			Assert.AreEqual(5, sel().Item2);

			arg = (2, 4);
			Assert.AreSame(sel(), sel());
			Assert.AreEqual(6, sel().Item2);
		}

	}
}
