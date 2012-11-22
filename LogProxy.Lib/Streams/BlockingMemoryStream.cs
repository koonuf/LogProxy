using System;
using System.IO;
using System.Threading;

namespace LogProxy.Lib.Streams
{
    /// <summary>
    /// Memory stream that blocks on read until data is written or it's "finished".
    /// </summary>
    public class BlockingMemoryStream : Stream
    {
        private byte[] dataBuffer = new byte[1024 * 2];
        private int bufferSize;
        private ManualResetEventSlim waitHandle = new ManualResetEventSlim(false);
        private readonly object syncLock = new object();
        private volatile bool finished;

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0 && !this.finished)
            {
                lock (this.syncLock)
                {
                    Utils.EnsureArraySize(ref this.dataBuffer, this.bufferSize + count, this.bufferSize);
                    Buffer.BlockCopy(buffer, offset, this.dataBuffer, this.bufferSize, count);
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
                    waitHandle.Wait();
                }

                lock (this.syncLock)
                {
                    count = Math.Min(count, this.bufferSize - (int)this.Position);
                    if (count > 0)
                    {
                        Buffer.BlockCopy(this.dataBuffer, (int)this.Position, buffer, offset, count);
                        this.Position += count;
                        return count;
                    }
                    else if (!this.finished)
                    {
                        this.waitHandle.Reset();
                    }
                    else
                    {
                        return 0;
                    }
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
            return Utils.CopyArray(this.dataBuffer, this.bufferSize);
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
            get { return true; }
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
    }
}
