using System;
using System.Threading.Tasks;

namespace LogJoint
{
    public static class DisposableAsync
    {
        public static async Task Using<T>(this T obj, Func<T, Task> body) where T : Postprocessing.IDisposableAsync
        {
            try
            {
                await body(obj);
            }
            finally
            {
                if (obj != null)
                {
                    await obj.Dispose();
                }
            }
        }

        public static async Task<R> Using<T, R>(this T obj, Func<T, Task<R>> body) where T: Postprocessing.IDisposableAsync
        {
            try
            {
                return await body(obj);
            }
            finally
            {
                if (obj != null)
                {
                    await obj.Dispose();
                }
            }
        }
    }
}
