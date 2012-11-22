using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Authentication;
using LogProxy.Lib.Http;
using LogProxy.Lib.Inspection;

namespace LogProxy.Lib.Sockets
{
    public class ServerWorker : WorkerBase
    {
        // chunks of client data to be sent to server
        private BlockingCollection<byte[]> toServerDataQueue = new BlockingCollection<byte[]>();

        // HTTP messages to the remote host, which client already started, but have not yet started on server side
        private ConcurrentQueue<HttpMessage> httpMessageQueue = new ConcurrentQueue<HttpMessage>();

        // HTTP message, which current bytes on the socket correspond to
        private HttpMessage currentMessage;

        private string remoteHost;
        private SocketWrapper workerSocket;
        private byte[] dataBuffer = new byte[DataBufferSize];

        private SocketWrapper clientSocket;
        private ClientWorker clientWorker;

        private volatile bool started;
        private readonly object syncLock = new object();
        IInspectorFactory inspectorFactory;

        public ServerWorker(ProxySettings settings, string remoteHost, ClientWorker clientWorker, SocketWrapper clientSocket)
            : base(settings)
        {
            this.remoteHost = remoteHost;
            this.clientWorker = clientWorker;
            this.clientSocket = clientSocket;
            this.inspectorFactory = settings.InspectorFactory;
        }

        public void EnqueueFromClientData(byte[] data)
        {
            if (!this.finishScheduled)
            {
                this.toServerDataQueue.Add(data);
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
                        errorCallback: () => { this.ScheduleFinish(); },
                        settings: this.Settings);

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
                    this.workerSocket.TransferToSecureServer(this.remoteHost);
                    this.clientSocket.FinishTransferToSecureClient(this.remoteHost);
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
            this.StartSendDataTask(targetSocket: this.workerSocket, sourceDataQueue: this.toServerDataQueue);
        }

        private void ServerReceive()
        {
            this.SocketReceive(this.workerSocket, this.dataBuffer, this.OnServerDataReceived);
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

                this.clientWorker.EnqueueFromServerData(data);
                this.UpdateCurrentMessage(data);
                this.ServerReceive();
            }
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

                data = Utils.GetOffsetContent(data, this.currentMessage.Response.ExtraContentOffset);

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

        protected override void BeforeFinishScheduled()
        {
            this.toServerDataQueue.CompleteAdding();
            if (this.workerSocket != null)
            {
                this.workerSocket.Close();
            }
        }

        public override void ScheduleFinish()
        {
            base.ScheduleFinish();

            if (this.clientWorker != null)
            {
                this.clientWorker.ScheduleFinish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.toServerDataQueue != null)
            {
                this.toServerDataQueue.Dispose();
            }

            if (this.workerSocket != null)
            {
                this.workerSocket.Dispose();
            }
        }
    }
}