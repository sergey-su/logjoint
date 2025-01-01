using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LogJoint
{
    class GeneratingStream : Stream
    {
        long length;
        byte filledWith;
        long position;

        public GeneratingStream(long length, byte filledWith)
        {
            this.length = length;
            this.filledWith = filledWith;
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

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int startOffset = offset;
            for (; count > 0 && position < length; --count, ++position, ++offset)
                buffer[offset] = filledWith;
            return offset - startOffset;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
