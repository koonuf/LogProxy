using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Sockets
{
    public class ClientWorker : WorkerBase
    {
        private SocketWrapper workerSocket;
        private byte[] dataBuffer = new byte[DataBufferSize];

        // used before current HTTP message's headers are received (before we know which host to send data to)
        private Queue<byte[]> fromClientDataQueue = new Queue<byte[]>();

        // chunks of server data to be sent to the client
        private BlockingCollection<byte[]> fromServerDataQueue = new BlockingCollection<byte[]>();

        // server workers per host, requested on this client socket
        private Dictionary<string, ServerWorker> serverWorkers = new Dictionary<string, ServerWorker>();

        // current data on client's stream/socket is associated with this HTTP request/response
        private HttpMessage currentHttpMessage;

        public ClientWorker(Socket workerSocket, ProxySettings settings)
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
                clientData = Utils.GetOffsetContent(clientData, this.currentHttpMessage.Request.ExtraContentOffset);

                this.ResetCurrentHttpMessage();
            }
        }

        private void EnqueueClientDataToServerSocket(byte[] httpMessageData)
        {
            if (!this.currentHttpMessage.Request.IsInitialized)
            {
                this.fromClientDataQueue.Enqueue(httpMessageData);
            }
            else
            {
                ServerWorker serverSocket = GetOrCreateServerSocket();

                if (!this.currentHttpMessage.ServerRelayInitiated)
                {
                    serverSocket.EnqueueHttpMessage(this.currentHttpMessage);
                    this.currentHttpMessage.ServerRelayInitiated = true;
                }

                while (this.fromClientDataQueue.Count > 0)
                {
                    serverSocket.EnqueueFromClientData(this.fromClientDataQueue.Dequeue());
                }

                serverSocket.EnqueueFromClientData(httpMessageData);

                serverSocket.Start();
            }
        }

        private void TransferToSecure()
        {
            this.workerSocket.StartTransferToSecureClient();
            this.GetOrCreateServerSocket().Start();
            this.ResetCurrentHttpMessage();
        }

        private ServerWorker GetOrCreateServerSocket()
        {
            string remoteHost = this.currentHttpMessage.Request.Host;

            ServerWorker serverSocket;
            if (!serverWorkers.TryGetValue(remoteHost, out serverSocket))
            {
                serverSocket = new ServerWorker(this.Settings, remoteHost, this, this.workerSocket);
                serverWorkers.Add(remoteHost, serverSocket);
            }

            return serverSocket;
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

            foreach (var serverSocketElement in this.serverWorkers.Where(s => !s.Value.FinishScheduled))
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