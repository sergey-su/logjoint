using System;
using System.Collections.Generic;

namespace LogJoint
{
	public static class Selectors
	{
		public static Func<R> Create<A1, R>(
			Func<A1> argSelector1,
			Func<A1, R> resultSelector)
		{
			var memoArg1 = default(A1);
			var cmp1 = GetEqualityComparer<A1>();
			R memoRet = default(R);
			bool firstEvaluation = true;
			return () =>
			{
				var arg1 = argSelector1();
				if (firstEvaluation || !cmp1.Equals(arg1, memoArg1))
				{
					firstEvaluation = false;
					memoRet = resultSelector(arg1);
					memoArg1 = arg1;
				}
				return memoRet;
			};
		}

		public static Func<R> Create<A1, A2, R>(
			Func<A1> argSelector1,
			Func<A2> argSelector2,
			Func<A1, A2, R> resultSelector)
		{
			var memoArg1 = default(A1);
			var cmp1 = GetEqualityComparer<A1>();
			var memoArg2 = default(A2);
			var cmp2 = GetEqualityComparer<A2>();
			R memoRet = default(R);
			bool firstEvaluation = true;
			return () =>
			{
				var arg1 = argSelector1();
				var arg2 = argSelector2();
				if (firstEvaluation || !cmp1.Equals(arg1, memoArg1) || !cmp2.Equals(arg2, memoArg2))
				{
					firstEvaluation = false;
					memoRet = resultSelector(arg1, arg2);
					memoArg1 = arg1;
					memoArg2 = arg2;
				}
				return memoRet;
			};
		}

		public static Func<R> Create<A1, A2, A3, R>(
			Func<A1> argSelector1,
			Func<A2> argSelector2,
			Func<A3> argSelector3,
			Func<A1, A2, A3, R> resultSelector)
		{
			var memoArg1 = default(A1);
			var cmp1 = GetEqualityComparer<A1>();
			var memoArg2 = default(A2);
			var cmp2 = GetEqualityComparer<A2>();
			var memoArg3 = default(A3);
			var cmp3 = GetEqualityComparer<A3>();
			R memoRet = default(R);
			bool firstEvaluation = true;
			return () =>
			{
				var arg1 = argSelector1();
				var arg2 = argSelector2();
				var arg3 = argSelector3();
				if (firstEvaluation || !cmp1.Equals(arg1, memoArg1) || !cmp2.Equals(arg2, memoArg2) || !cmp3.Equals(arg3, memoArg3))
				{
					firstEvaluation = false;
					memoRet = resultSelector(arg1, arg2, arg3);
					memoArg1 = arg1;
					memoArg2 = arg2;
					memoArg3 = arg3;
				}
				return memoRet;
			};
		}

		internal static IEqualityComparer<T> GetEqualityComparer<T>()
		{
			return EqualityComparer<T>.Default;
		}
	}

	public static class Updaters
	{
		public static Action Create<A1>(Func<A1> argSelector1, Action<A1, A1> update)
		{
			var prevA1 = default(A1);
			bool firstUpdate = true;
			var keyComparer = Selectors.GetEqualityComparer<A1>();
			return () =>
			{
				var a1 = argSelector1();
				if (firstUpdate || !keyComparer.Equals(a1, prevA1))
				{
					firstUpdate = false;
					prevA1 = a1;
					update(a1, prevA1);
				}
			};
		}

		public static Action Create<A1>(Func<A1> argSelector1, Action<A1> update)
		{
			return Create(argSelector1, (key, oldKey) => update(key));
		}
	};
}
