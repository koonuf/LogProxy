using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Sockets
{
    public class ClientWorkerSocket : WorkerSocketBase
    {
        private const string HostHeaderName = "Host";

        private SocketWrapper workerSocket;

        private byte[] dataBuffer = new byte[DataBufferSize];

        private Queue<byte[]> fromClientDataQueue = new Queue<byte[]>();
        private BlockingCollection<byte[]> fromServerDataQueue = new BlockingCollection<byte[]>();

        private Dictionary<string, ServerWorkerSocket> serverSockets = new Dictionary<string, ServerWorkerSocket>();
        private HttpMessage currentHttpMessage;

        public ClientWorkerSocket(Socket workerSocket, ProxySettings settings)
            : base(settings)
        {
            this.workerSocket = new SocketWrapper(workerSocket, settings);
        }

        public void StartRelay()
        {
            this.ClientReceive();
            this.StartSendToClientTask();
        }

        private void ClientReceive()
        {
            this.SocketReceive(this.workerSocket, this.dataBuffer, this.OnClientDataReceived);
        }

        private void StartSendToClientTask()
        {
            this.StartSendDataTask(targetSocket: this.workerSocket, sourceDataQueue: this.fromServerDataQueue);
        }

        private void OnClientDataReceived(IAsyncResult result)
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

                this.UpdateCurrentClientHttpMessage(data);
                this.ClientReceive();
            }
        }

        private void UpdateCurrentClientHttpMessage(byte[] clientData)
        {
            this.EnsureCurrentHttpMessage();

            while (clientData != null)
            {
                byte[] httpMessageData = this.currentHttpMessage.AddRequestData(clientData);

                if (this.currentHttpMessage.Request.IsRequestToSecureConnection)
                {
                    this.TransferToSecure();
                    return;
                }

                this.EnqueueClientDataToServerSocket(httpMessageData);

                if (!this.currentHttpMessage.Request.FinishedLoading)
                {
                    return;
                }

                // get data of next request
                clientData = GetOffsetContent(clientData, this.currentHttpMessage.Request.ExtraContentOffset);

                this.ResetCurrentHttpMessage();
            }
        }

        private ServerWorkerSocket GetOrCreateServerSocket()
        {
            string remoteHost = this.currentHttpMessage.Request.Host;
            
            ServerWorkerSocket serverSocket;
            if (!serverSockets.TryGetValue(remoteHost, out serverSocket))
            {
                serverSocket = new ServerWorkerSocket(this.Settings, remoteHost, this, this.workerSocket);
                serverSockets.Add(remoteHost, serverSocket);
            }

            return serverSocket;
        }

        private void EnqueueClientDataToServerSocket(byte[] httpMessageData)
        {
            if (!this.currentHttpMessage.Request.IsInitialized)
            {
                this.fromClientDataQueue.Enqueue(httpMessageData);
            }
            else
            {
                ServerWorkerSocket serverSocket = GetOrCreateServerSocket();

                if (!this.currentHttpMessage.ServerRelayInitiated)
                {
                    serverSocket.EnqueueHttpMessage(this.currentHttpMessage);
                    this.currentHttpMessage.ServerRelayInitiated = true;
                }

                while (this.fromClientDataQueue.Count > 0)
                {
                    serverSocket.EnqueueData(this.fromClientDataQueue.Dequeue());
                }

                serverSocket.EnqueueData(httpMessageData);

                serverSocket.Start();
            }
        }

        private void TransferToSecure()
        {
            this.workerSocket.StartTransferToSecureAsClient();
            this.GetOrCreateServerSocket().Start();
            this.ResetCurrentHttpMessage();
        }

        private void ResetCurrentHttpMessage()
        {
            this.currentHttpMessage = new HttpMessage(this.Settings);
        }

        private void EnsureCurrentHttpMessage()
        {
            if (this.currentHttpMessage == null)
            {
                this.ResetCurrentHttpMessage();
            }
        }

        public void EnqueueFromServerData(byte[] data)
        {
            if (!this.finishScheduled)
            {
                this.fromServerDataQueue.Add(data);
            }
        }

        public override void ScheduleFinish()
        {
            base.ScheduleFinish();

            foreach (var serverSocketElement in this.serverSockets.Where(s => !s.Value.FinishScheduled))
            {
                serverSocketElement.Value.ScheduleFinish();
            }
        }

        protected override void BeforeFinishScheduled()
        {
            this.fromServerDataQueue.CompleteAdding();
            this.workerSocket.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            fromServerDataQueue.Dispose();
            this.workerSocket.Dispose();
        }
    }
}