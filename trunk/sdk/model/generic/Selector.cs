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

        public static Func<R> Create<A1, A2, A3, A4, R>(
            Func<A1> argSelector1,
            Func<A2> argSelector2,
            Func<A3> argSelector3,
            Func<A4> argSelector4,
            Func<A1, A2, A3, A4, R> resultSelector)
        {
            var memoArg1 = default(A1);
            var cmp1 = GetEqualityComparer<A1>();
            var memoArg2 = default(A2);
            var cmp2 = GetEqualityComparer<A2>();
            var memoArg3 = default(A3);
            var cmp3 = GetEqualityComparer<A3>();
            var memoArg4 = default(A4);
            var cmp4 = GetEqualityComparer<A4>();
            R memoRet = default(R);
            bool firstEvaluation = true;
            return () =>
            {
                var arg1 = argSelector1();
                var arg2 = argSelector2();
                var arg3 = argSelector3();
                var arg4 = argSelector4();
                if (firstEvaluation || !cmp1.Equals(arg1, memoArg1) || !cmp2.Equals(arg2, memoArg2) || !cmp3.Equals(arg3, memoArg3) || !cmp4.Equals(arg4, memoArg4))
                {
                    firstEvaluation = false;
                    memoRet = resultSelector(arg1, arg2, arg3, arg4);
                    memoArg1 = arg1;
                    memoArg2 = arg2;
                    memoArg3 = arg3;
                    memoArg4 = arg4;
                }
                return memoRet;
            };
        }

        public static Func<R> Create<A1, A2, A3, A4, A5, R>(
            Func<A1> argSelector1,
            Func<A2> argSelector2,
            Func<A3> argSelector3,
            Func<A4> argSelector4,
            Func<A5> argSelector5,
            Func<A1, A2, A3, A4, A5, R> resultSelector)
        {
            var memoArg1 = default(A1);
            var cmp1 = GetEqualityComparer<A1>();
            var memoArg2 = default(A2);
            var cmp2 = GetEqualityComparer<A2>();
            var memoArg3 = default(A3);
            var cmp3 = GetEqualityComparer<A3>();
            var memoArg4 = default(A4);
            var cmp4 = GetEqualityComparer<A4>();
            var memoArg5 = default(A5);
            var cmp5 = GetEqualityComparer<A5>();
            R memoRet = default(R);
            bool firstEvaluation = true;
            return () =>
            {
                var arg1 = argSelector1();
                var arg2 = argSelector2();
                var arg3 = argSelector3();
                var arg4 = argSelector4();
                var arg5 = argSelector5();
                if (firstEvaluation || !cmp1.Equals(arg1, memoArg1) || !cmp2.Equals(arg2, memoArg2) || !cmp3.Equals(arg3, memoArg3) || !cmp4.Equals(arg4, memoArg4) || !cmp5.Equals(arg5, memoArg5))
                {
                    firstEvaluation = false;
                    memoRet = resultSelector(arg1, arg2, arg3, arg4, arg5);
                    memoArg1 = arg1;
                    memoArg2 = arg2;
                    memoArg3 = arg3;
                    memoArg4 = arg4;
                    memoArg5 = arg5;
                }
                return memoRet;
            };
        }

        public static Func<R> Create<A1, A2, A3, A4, A5, A6, R>(
            Func<A1> argSelector1,
            Func<A2> argSelector2,
            Func<A3> argSelector3,
            Func<A4> argSelector4,
            Func<A5> argSelector5,
            Func<A6> argSelector6,
            Func<A1, A2, A3, A4, A5, A6, R> resultSelector)
        {
            var memoArg1 = default(A1);
            var cmp1 = GetEqualityComparer<A1>();
            var memoArg2 = default(A2);
            var cmp2 = GetEqualityComparer<A2>();
            var memoArg3 = default(A3);
            var cmp3 = GetEqualityComparer<A3>();
            var memoArg4 = default(A4);
            var cmp4 = GetEqualityComparer<A4>();
            var memoArg5 = default(A5);
            var cmp5 = GetEqualityComparer<A5>();
            var memoArg6 = default(A6);
            var cmp6 = GetEqualityComparer<A6>();
            R memoRet = default(R);
            bool firstEvaluation = true;
            return () =>
            {
                var arg1 = argSelector1();
                var arg2 = argSelector2();
                var arg3 = argSelector3();
                var arg4 = argSelector4();
                var arg5 = argSelector5();
                var arg6 = argSelector6();
                if (firstEvaluation || !cmp1.Equals(arg1, memoArg1) || !cmp2.Equals(arg2, memoArg2) || !cmp3.Equals(arg3, memoArg3) || !cmp4.Equals(arg4, memoArg4) || !cmp5.Equals(arg5, memoArg5) || !cmp6.Equals(arg6, memoArg6))
                {
                    firstEvaluation = false;
                    memoRet = resultSelector(arg1, arg2, arg3, arg4, arg5, arg6);
                    memoArg1 = arg1;
                    memoArg2 = arg2;
                    memoArg3 = arg3;
                    memoArg4 = arg4;
                    memoArg5 = arg5;
                    memoArg6 = arg6;
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
                    var savePrevA1 = prevA1;
                    prevA1 = a1;
                    update(a1, savePrevA1);
                }
            };
        }

        public static Action Create<A1>(Func<A1> argSelector1, Action<A1> update)
        {
            return Create(argSelector1, (key, oldKey) => update(key));
        }

        public static Action Create<A1, A2>(Func<A1> argSelector1, Func<A2> argSelector2, Action<A1, A2, A1, A2> update)
        {
            return Create(
                Selectors.Create(
                    argSelector1,
                    argSelector2,
                    (a1, a2) => (a1, a2)
                ),
                (key, prevKey) => update(key.a1, key.a2, prevKey.a1, prevKey.a2)
            );
        }

        public static Action Create<A1, A2>(Func<A1> argSelector1, Func<A2> argSelector2, Action<A1, A2> update)
        {
            return Create(argSelector1, argSelector2, (a1, a2, oldA1, oldA2) => update(a1, a2));
        }

        public static Action Create<A1, A2, A3>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Action<A1, A2, A3, A1, A2, A3> update)
        {
            return Create(
                Selectors.Create(
                    argSelector1,
                    argSelector2,
                    argSelector3,
                    (a1, a2, a3) => (a1, a2, a3)
                ),
                (key, prevKey) => update(key.a1, key.a2, key.a3, prevKey.a1, prevKey.a2, prevKey.a3)
            );
        }

        public static Action Create<A1, A2, A3>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Action<A1, A2, A3> update)
        {
            return Create(argSelector1, argSelector2, argSelector3, (a1, a2, a3, oldA1, oldA2, oldA3) => update(a1, a2, a3));
        }

        public static Action Create<A1, A2, A3, A4>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Action<A1, A2, A3, A4, A1, A2, A3, A4> update)
        {
            return Create(
                Selectors.Create(
                    argSelector1,
                    argSelector2,
                    argSelector3,
                    argSelector4,
                    (a1, a2, a3, a4) => (a1, a2, a3, a4)
                ),
                (key, prevKey) => update(key.a1, key.a2, key.a3, key.a4, prevKey.a1, prevKey.a2, prevKey.a3, prevKey.a4)
            );
        }

        public static Action Create<A1, A2, A3, A4>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Action<A1, A2, A3, A4> update)
        {
            return Create(argSelector1, argSelector2, argSelector3, argSelector4, (a1, a2, a3, a4, oldA1, oldA2, oldA3, oldA4) => update(a1, a2, a3, a4));
        }

        public static Action Create<A1, A2, A3, A4, A5>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Func<A5> argSelector5, Action<A1, A2, A3, A4, A5, A1, A2, A3, A4, A5> update)
        {
            return Create(
                Selectors.Create(
                    argSelector1,
                    argSelector2,
                    argSelector3,
                    argSelector4,
                    argSelector5,
                    (a1, a2, a3, a4, a5) => (a1, a2, a3, a4, a5)
                ),
                (key, prevKey) => update(key.a1, key.a2, key.a3, key.a4, key.a5, prevKey.a1, prevKey.a2, prevKey.a3, prevKey.a4, prevKey.a5)
            );
        }

        public static Action Create<A1, A2, A3, A4, A5>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Func<A5> argSelector5, Action<A1, A2, A3, A4, A5> update)
        {
            return Create(argSelector1, argSelector2, argSelector3, argSelector4, argSelector5, (a1, a2, a3, a4, a5, oldA1, oldA2, oldA3, oldA4, oldA5) => update(a1, a2, a3, a4, a5));
        }

        public static Action Create<A1, A2, A3, A4, A5, A6>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Func<A5> argSelector5, Func<A6> argSelector6, Action<A1, A2, A3, A4, A5, A6, A1, A2, A3, A4, A5, A6> update)
        {
            return Create(
                Selectors.Create(
                    argSelector1,
                    argSelector2,
                    argSelector3,
                    argSelector4,
                    argSelector5,
                    argSelector6,
                    (a1, a2, a3, a4, a5, a6) => (a1, a2, a3, a4, a5, a6)
                ),
                (key, prevKey) => update(key.a1, key.a2, key.a3, key.a4, key.a5, key.a6, prevKey.a1, prevKey.a2, prevKey.a3, prevKey.a4, prevKey.a5, prevKey.a6)
            );
        }

        public static Action Create<A1, A2, A3, A4, A5, A6>(Func<A1> argSelector1, Func<A2> argSelector2, Func<A3> argSelector3, Func<A4> argSelector4, Func<A5> argSelector5, Func<A6> argSelector6, Action<A1, A2, A3, A4, A5, A6> update)
        {
            return Create(argSelector1, argSelector2, argSelector3, argSelector4, argSelector5, argSelector6, (a1, a2, a3, a4, a5, a6, oldA1, oldA2, oldA3, oldA4, oldA5, oldA6) => update(a1, a2, a3, a4, a5, a6));
        }
    };
}
