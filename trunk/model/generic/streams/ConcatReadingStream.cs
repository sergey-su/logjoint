using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint
{
    public class ConcatReadingStream : Stream
    {
        public void Update(IEnumerable<Stream> stms)
        {
            streams.Clear();
            streams.AddRange(stms);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Stream s in streams)
                    s.Dispose();
            }
            base.Dispose(disposing);
        }

        public override long Length
        {
            get
            {
                long ret = 0;
                foreach (Stream s in streams)
                    ret += s.Length;
                return ret;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("position", "Position cannot be negative");
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long tmp = 0;
            bool reading = false;
            int readTotal = 0;
            foreach (Stream s in streams)
            {
                tmp += s.Length;

                if (reading)
                {
                    s.Position = 0;
                }
                else if (position < tmp)
                {
                    s.Position = position - (tmp - s.Length);
                    reading = true;
                }

                if (!reading)
                {
                    continue;
                }
                int read = s.Read(buffer, offset, count);
                readTotal += read;
                count -= read;
                offset += read;
                if (count <= 0)
                {
                    break;
                }
            }
            position += readTotal;
            return readTotal;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            ThrowModificationAttemptException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowModificationAttemptException();
        }

        void ThrowModificationAttemptException()
        {
            throw new InvalidOperationException(string.Format("The stream of type '{0}' cannot be modified", GetType().ToString()));
        }

        List<Stream> streams = new List<Stream>();
        long position;
    };
}
