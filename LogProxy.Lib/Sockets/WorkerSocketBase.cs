using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LogProxy.Lib.Sockets
{
    public abstract class WorkerSocketBase : IDisposable
    {
        public const int DataBufferSize = 2048;

        protected volatile bool receiveFinished;
        protected volatile bool sendFinished;
        protected volatile bool finishScheduled;
        protected volatile bool disposed;

        private readonly object syncLock = new object();

        private Guid id;

        public WorkerSocketBase(ProxySettings settings)
        {
            this.Settings = settings;
            this.id = Guid.NewGuid();
        }

        protected ProxySettings Settings { get; private set; }

        public bool FinishScheduled
        {
            get { return this.finishScheduled; }
        }

        protected void SocketReceive(SocketWrapper socket, byte[] buffer, AsyncCallback callback)
        {
            if (this.finishScheduled)
            {
                this.receiveFinished = true;
                return;
            }

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, callback);
            }
            catch (SocketException)
            {
                this.receiveFinished = true;
                this.ScheduleFinish();
            }
            catch (IOException)
            {
                this.receiveFinished = true;
                this.ScheduleFinish();
            }
        }

        protected int EndSocketReceive(SocketWrapper socket, IAsyncResult result)
        {
            int bytesReceived = 0;

            if (this.finishScheduled)
            {
                this.receiveFinished = true;
            }
            else
            {
                try
                {
                    bytesReceived = socket.EndReceive(result);
                    if (bytesReceived == 0)
                    {
                        this.receiveFinished = true;
                        this.ScheduleFinish();
                    }
                }
                catch (SocketException)
                {
                    this.receiveFinished = true;
                    this.ScheduleFinish();
                }
                catch (IOException)
                {
                    this.receiveFinished = true;
                    this.ScheduleFinish();
                }
            }

            return bytesReceived;
        }

        protected void StartSendDataTask(SocketWrapper targetSocket, BlockingCollection<byte[]> sourceDataQueue)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (var dataBlock in sourceDataQueue.GetConsumingEnumerable())
                    {
                        targetSocket.Send(dataBlock);
                    }
                }
                catch (SocketException)
                {
                }
                catch (IOException)
                { 
                }

                this.sendFinished = true;
                this.ScheduleFinish();
            }); 
        }

        protected static byte[] GetOffsetContent(byte[] data, int offsetLength)
        {
            if (offsetLength > 0)
            {
                return Utils.CopyArray(data, data.Length - offsetLength, offsetLength);
            }

            return null;
        }

        protected abstract void BeforeFinishScheduled();

        public virtual void ScheduleFinish()
        {
            this.finishScheduled = true;

            if (!this.disposed)
            {
                lock (this.syncLock)
                {
                    if (!this.disposed)
                    {
                        this.BeforeFinishScheduled();
                        if (this.sendFinished && this.receiveFinished)
                        {
                            this.Dispose();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                lock (this.syncLock)
                {
                    if (!this.disposed)
                    {
                        this.Dispose(true);
                        GC.SuppressFinalize(this);
                        this.disposed = true;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
