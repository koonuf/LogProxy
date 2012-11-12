using System;
using System.IO;
using System.Threading;

namespace LogProxy.Lib
{
    public class ContentStream : Stream
    {
        private byte[] buffer = new byte[1024 * 2];
        private int bufferSize;
        private ManualResetEvent waitHandle = new ManualResetEvent(false);
        private readonly object syncLock = new object();
        private volatile bool finished;

        public void AddContent(byte[] data, int offset, int count)
        {
            if (count > 0 && !this.finished)
            {
                lock (this.syncLock)
                {
                    Utils.EnsureArraySize(ref this.buffer, this.bufferSize + count, this.bufferSize);
                    Buffer.BlockCopy(data, offset, this.buffer, this.bufferSize, count);
                    this.bufferSize += count;
                    this.waitHandle.Set();
                }
            }
        }

        public void Finish()
        {
            this.finished = true;
            this.waitHandle.Set();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (true)
            {
                if (!this.finished)
                {
                    waitHandle.WaitOne();
                }

                lock (this.syncLock)
                {
                    count = Math.Min(count, this.bufferSize - (int)this.Position);
                    if (count >= 0)
                    {
                        Buffer.BlockCopy(this.buffer, (int)this.Position, buffer, offset, count);
                        this.Position += count;
                    }
                    else
                    {
                        this.waitHandle.Reset();
                    }

                    return count;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.waitHandle != null)
            {
                this.waitHandle.Set();
                this.waitHandle.Dispose();
                this.waitHandle = null;
            }
        }

        public byte[] ToArray()
        {
            return Utils.CopyArray(this.buffer, this.bufferSize);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
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
            get { return -1; }
        }

        public override long Position { get; set; }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return -1;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
