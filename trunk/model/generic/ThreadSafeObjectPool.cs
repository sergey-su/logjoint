using System;
using System.Collections.Concurrent;

namespace LogJoint
{
    public class ThreadSafeObjectPool<T>
    {
        public ThreadSafeObjectPool(Func<ThreadSafeObjectPool<T>, T> factoryMethod)
        {
            if (factoryMethod == null)
                throw new ArgumentNullException();
            this.factoryMethod = factoryMethod;
        }

        public T LockAndGet()
        {
            T ret;
            if (freeObjects.TryPop(out ret))
                return ret;
            ret = factoryMethod(this);
            return ret;
        }

        public void Release(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            freeObjects.Push(obj);
        }

        public void Clear()
        {
            freeObjects.Clear();
        }

        public int FreeObjectsCount
        {
            get { return freeObjects.Count; }
        }

        readonly Func<ThreadSafeObjectPool<T>, T> factoryMethod;
        readonly ConcurrentStack<T> freeObjects = new ConcurrentStack<T>();
    }

    public class ThreadSafeWeakReferencesPool<T> where T : class
    {
        public ThreadSafeWeakReferencesPool(Func<ThreadSafeWeakReferencesPool<T>, T> factoryMethod)
        {
            if (factoryMethod == null)
                throw new ArgumentNullException();
            this.factoryMethod = factoryMethod;
        }

        public T LockAndGet()
        {
            for (; ; )
            {
                WeakReference wref;
                if (!freeObjects.TryPop(out wref))
                    break;
                T ret = wref.Target as T;
                if (ret != null)
                    return ret;
            }
            return factoryMethod(this);
        }

        public void Release(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            freeObjects.Push(new WeakReference(obj));
        }

        readonly Func<ThreadSafeWeakReferencesPool<T>, T> factoryMethod;
        readonly ConcurrentStack<WeakReference> freeObjects = new ConcurrentStack<WeakReference>();
    }
}
