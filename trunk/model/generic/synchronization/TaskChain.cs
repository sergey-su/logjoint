using System;
using System.Threading.Tasks;

namespace LogJoint
{

    /// <summary>
    /// Runs added tasks sequentially. Has async Dispose that waits on pending tasks.
    /// Single-threaded.
    /// </summary>
    class TaskChain
    {
        Task tail = Task.CompletedTask;

        public Task AddTask(Func<Task> task)
        {
            tail = Add(tail, task);
            return tail;
        }

        public Task Dispose() => tail;

        private static async Task Add(Task tail, Func<Task> task)
        {
            await tail;
            await task();
        }
    }
}
