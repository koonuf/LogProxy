using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LogProxy.Lib.Sockets
{
    public abstract class WorkerBase : IDisposable
    {
        protected const int DataBufferSize = 2048;

        protected volatile bool receiveFinished;
        protected volatile bool sendFinished;
        protected volatile bool finishScheduled;
        protected volatile bool disposed;

        private readonly object syncLock = new object();

        public WorkerBase(ProxySettings settings)
        {
            this.Settings = settings;
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
                this.FinishReceive();
                return;
            }

            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, callback);
            }
            catch (SocketException)
            {
                this.FinishReceive();
            }
            catch (IOException)
            {
                this.FinishReceive();
            }
        }

        protected int EndSocketReceive(SocketWrapper socket, IAsyncResult result)
        {
            int bytesReceived = 0;

            if (this.finishScheduled)
            {
                this.FinishReceive();
            }
            else
            {
                try
                {
                    bytesReceived = socket.EndReceive(result);
                    if (bytesReceived == 0)
                    {
                        this.FinishReceive();
                    }
                }
                catch (SocketException)
                {
                    this.FinishReceive();
                }
                catch (IOException)
                {
                    this.FinishReceive();
                }
            }

            return bytesReceived;
        }

        private void FinishReceive()
        {
            this.receiveFinished = true;
            this.ScheduleFinish();
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
