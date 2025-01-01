using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace LogJoint
{
    // A read-only stream that caches the reads. Underlying stream must be immutable.
    public class CachingStream : DelegatingStream
    {
        private readonly MemoryCache cache;
        private readonly long pageSize;
        private long readFromCache;
        private long readFromUnderlyingStream;

        public CachingStream(long sizeLimit, Stream stream = null, bool ownStream = false, int pageSize = 4096) : base(stream, ownStream)
        {
            this.pageSize = pageSize;
            this.cache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = sizeLimit
            });
        }

        public CachingStream(MemoryCache cache, Stream stream = null, bool ownStream = false, int pageSize = 4096) : base(stream, ownStream)
        {
            this.pageSize = pageSize;
            this.cache = cache;
        }

        public long ReadFromCache => readFromCache;
        public long ReadFromUnderlyingStream => readFromUnderlyingStream;

        public override bool CanWrite => false;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await base.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadImpl(new Memory<byte>(buffer, offset, count),
                pageData => new ValueTask<int>(base.Read(pageData, 0, pageData.Length))).Result;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return ReadImpl(buffer, pageData => base.ReadAsync(pageData, cancellationToken));
        }

        private async ValueTask<int> ReadImpl(Memory<byte> buffer, Func<byte[], ValueTask<int>> readPage)
        {
            int bufferWritten = 0;
            long beginPosition = Position;
            long endPosition = beginPosition + buffer.Length;
            for (long page = ToPageIndex(beginPosition); page <= ToPageIndex(endPosition - 1); ++page)
            {
                long pagePosition = ToStreamPosition(page);
                byte[] pageData;
                bool cacheHit = cache.TryGetValue(page, out var pageDataObj);
                if (cacheHit)
                {
                    pageData = (byte[])pageDataObj;
                }
                else
                {
                    pageData = new byte[pageSize];
                    Position = pagePosition;
                    int pageRead = await readPage(pageData);
                    if (pageRead != pageSize)
                    {
                        var tailPageData = new byte[pageRead];
                        new Memory<byte>(pageData, 0, pageRead).CopyTo(tailPageData);
                        pageData = tailPageData;
                    }
                    cache.Set(page, pageData, new MemoryCacheEntryOptions()
                    {
                        Size = pageSize,
                    });
                }
                int copied;
                if (pagePosition >= beginPosition)
                {
                    var pageSlice = new Memory<byte>(pageData)[..Math.Min(pageData.Length, (int)(endPosition - pagePosition))];
                    pageSlice.CopyTo(buffer[(int)(pagePosition - beginPosition)..]);
                    copied = pageSlice.Length;
                }
                else
                {
                    var pageSlice = new Memory<byte>(pageData)[(int)(beginPosition - pagePosition)..];
                    pageSlice = pageSlice[..Math.Min(pageSlice.Length, buffer.Length)];
                    pageSlice.CopyTo(buffer);
                    copied = pageSlice.Length;
                }
                bufferWritten += copied;
                if (cacheHit)
                    readFromCache += copied;
                else
                    readFromUnderlyingStream += copied;
            }
            Position = endPosition;
            return bufferWritten;
        }

        long ToPageIndex(long streamPosition) => streamPosition / pageSize;

        long ToStreamPosition(long page) => page * pageSize;
    }
}
