using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Authentication;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Sockets
{
    public class ServerWorkerSocket : WorkerSocketBase
    {
        private BlockingCollection<byte[]> dataQueue = new BlockingCollection<byte[]>();
        private ConcurrentQueue<HttpMessage> httpMessageQueue = new ConcurrentQueue<HttpMessage>();
        private HttpMessage currentMessage;
        private string remoteHost;
        private SocketWrapper workerSocket;
        private SocketWrapper clientSocket;
        private byte[] dataBuffer = new byte[DataBufferSize];
        private ClientWorkerSocket clientWorkerSocket;
        private volatile bool started;

        private readonly object syncLock = new object();

        public ServerWorkerSocket(ProxySettings settings, string remoteHost, ClientWorkerSocket clientWorkerSocket, SocketWrapper clientSocket)
            : base(settings)
        {
            this.remoteHost = remoteHost;
            this.clientWorkerSocket = clientWorkerSocket;
            this.clientSocket = clientSocket;
        }

        public void EnqueueData(byte[] data)
        {
            if (!this.finishScheduled)
            {
                this.dataQueue.Add(data);
            }
        }

        public void EnqueueHttpMessage(HttpMessage message)
        {
            this.httpMessageQueue.Enqueue(message);
        }

        public void Start()
        {
            if (!started)
            {
                var connector = new HostConnector(
                        host: this.remoteHost,
                        secure: this.clientSocket.IsSecure,
                        connectionMadeCallback: OnConnectedToServer,
                        errorCallback: () => { this.ScheduleFinish(); });

                connector.StartConnecting();
                started = true;
            }
        }

        private void OnConnectedToServer(Socket workerSocket)
        {
            this.workerSocket = new SocketWrapper(workerSocket, this.Settings);

            if (this.finishScheduled)
            {
                this.workerSocket.Close();
                return;
            }

            if (this.clientSocket.IsSecure)
            {
                try
                {
                    this.workerSocket.TransferToSecureAsServer(this.remoteHost);
                    this.clientSocket.EndTransferToSecureAsClient(this.remoteHost);
                }
                catch (AuthenticationException)
                {
                    this.ScheduleFinish();
                    return;
                }
                catch (SocketException)
                {
                    this.ScheduleFinish();
                    return;
                }
            }

            this.StartSendToServerTask();
            this.ServerReceive();
        }

        private void StartSendToServerTask()
        {
            this.StartSendDataTask(
                targetSocket: this.workerSocket,
                sourceDataQueue: this.dataQueue);
        }

        private void ServerReceive()
        {
            this.SocketReceive(this.workerSocket, this.dataBuffer, this.OnServerDataReceived);
        }

        private void UpdateCurrentMessage(byte[] data)
        {
            while (data != null)
            {
                this.EnsureCurrentMessage();

                this.currentMessage.AddResponseData(data);

                if (!this.currentMessage.Response.FinishedLoading)
                {
                    return;
                }

                data = GetOffsetContent(data, this.currentMessage.Response.ExtraContentOffset);

                if (this.currentMessage.Response.IsContinueResponse)
                {
                    this.currentMessage.Response = new HttpResponseInfo();
                }
                else
                {
                    this.StopCurrentMessage();
                    this.currentMessage = null;
                }
            }
        }

        private void StopCurrentMessage()
        {
            if (this.currentMessage != null)
            {
                this.currentMessage.Stop();
                if (this.Settings != null && this.Settings.Logger != null)
                {
                    this.Settings.Logger.LogMessageFinished(this.currentMessage);
                }
            }
        }

        private void EnsureCurrentMessage()
        {
            if (this.currentMessage == null)
            {
                HttpMessage message;
                if (!this.httpMessageQueue.TryDequeue(out message))
                {
                    throw new InvalidOperationException("No messages in the queue to write response to");
                }

                this.currentMessage = message;
            }
        }

        private void OnServerDataReceived(IAsyncResult result)
        {
            int bytesRead = this.EndSocketReceive(this.workerSocket, result);

            if (bytesRead > 0)
            {
                byte[] data;
                if (bytesRead < this.dataBuffer.Length)
                {
                    data = Utils.CopyArray(this.dataBuffer, bytesRead);
                }
                else
                {
                    data = this.dataBuffer;
                    this.dataBuffer = new byte[DataBufferSize];
                }

                this.clientWorkerSocket.EnqueueFromServerData(data);
                this.UpdateCurrentMessage(data);
                this.ServerReceive();
            }
        }

        protected override void BeforeFinishScheduled()
        {
            this.dataQueue.CompleteAdding();
            if (this.workerSocket != null)
            {
                this.workerSocket.Close();
            }
        }

        public override void ScheduleFinish()
        {
            base.ScheduleFinish();

            if (this.clientWorkerSocket != null)
            {
                this.clientWorkerSocket.ScheduleFinish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.dataQueue != null)
            {
                this.dataQueue.Dispose();
            }

            if (this.workerSocket != null)
            {
                this.workerSocket.Dispose();
            }
        }
    }
}