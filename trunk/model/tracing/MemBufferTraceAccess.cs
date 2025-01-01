using System.IO;

namespace LogJoint
{
    public interface IMemBufferTraceAccess
    {
        void ClearMemBufferAndGetCurrentContents(TextWriter writer);
    }

    public class MemBufferTraceAccess : IMemBufferTraceAccess
    {
        void IMemBufferTraceAccess.ClearMemBufferAndGetCurrentContents(TextWriter writer)
        {
            var traceListener = TraceListener.LastInstance;
            if (traceListener == null)
                return;
            var entries = traceListener.ClearMemBufferAndGetCurrentEntries();
            entries.ForEach(e => e.Write(writer));
        }
    };
}
