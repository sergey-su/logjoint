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

        public void AddTask(Task task)
        {
            tail = Add(tail, task);
        }
        public void AddTask(Func<Task> task)
        {
            tail = Add(tail, task());
        }

        public Task Dispose() => tail;

        private static async Task Add(Task tail, Task task)
        {
            await tail;
            await task;
        }
    }
}
