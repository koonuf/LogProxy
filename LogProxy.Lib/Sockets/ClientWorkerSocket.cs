using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private HttpMessage currentMessage;

        public ClientWorkerSocket(Socket workerSocket, ProxySettings settings)
            : base(settings)
        {
            this.workerSocket = new SocketWrapper(workerSocket, settings);
        }

        public void EnqueueData(byte[] data)
        {
            if (!this.finishScheduled)
            {
                this.fromServerDataQueue.Add(data);
            }
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
            this.StartSendDataTask(
                targetSocket: this.workerSocket, 
                sourceDataQueue: this.fromServerDataQueue);
        }

        private void OnClientDataReceived(IAsyncResult result)
        {
            int bytesRead;
            this.EndSocketReceive(this.workerSocket, result, out bytesRead);

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

                this.UpdateCurrentClientMessage(data);
                this.ClientReceive();
            }
        }

        private void UpdateCurrentClientMessage(byte[] clientData)
        {
            this.EnsureClientMessage();

            while (clientData != null)
            {
                byte[] messageData = this.currentMessage.AddRequestData(clientData);

                if (this.currentMessage.Request.IsRequestToSecureConnection)
                {
                    this.TransferToSecure();
                    return;
                }

                this.TryEnqueueCurrentMessageToServerSocket(messageData);

                if (!this.currentMessage.Request.FinishedLoading)
                {
                    return;
                }

                clientData = GetOffsetContent(clientData, this.currentMessage.Request.ExtraContentOffset);
                this.ResetCurrentMessage();
            }
        }

        private void TransferToSecure()
        {
            this.workerSocket.StartTransferToSecureAsClient();
            ServerWorkerSocket serverSocket;

            if (CreateServerSocket(out serverSocket))
            {
                try
                {
                    this.workerSocket.EndTransferToSecureAsClient(this.currentMessage.Request.Host);
                }
                catch (AuthenticationException)
                {
                    this.ScheduleFinish();
                }
                catch (SocketException)
                {
                    this.ScheduleFinish();
                }
            }
            else
            {
                serverSocket.Start();
            }

            this.ResetCurrentMessage();
        }

        private bool CreateServerSocket(out ServerWorkerSocket serverSocket)
        {
            string remoteHost = this.currentMessage.Request.Host;

            if (!serverSockets.TryGetValue(remoteHost, out serverSocket))
            {
                serverSocket = new ServerWorkerSocket(this.Settings, remoteHost, this, this.workerSocket);
                serverSockets.Add(remoteHost, serverSocket);
                return false;
            }

            return true;
        }

        private void TryEnqueueCurrentMessageToServerSocket(byte[] messageData)
        {
            if (!this.currentMessage.Request.IsInitialized)
            {
                this.fromClientDataQueue.Enqueue(messageData);
            }
            else
            {
                ServerWorkerSocket serverSocket;
                bool serverSocketStarted = CreateServerSocket(out serverSocket);

                if (!this.currentMessage.ServerRelayInitiated)
                {
                    serverSocket.EnqueueMessage(this.currentMessage);
                    this.currentMessage.ServerRelayInitiated = true;
                }

                while (this.fromClientDataQueue.Count > 0)
                {
                    serverSocket.EnqueueData(this.fromClientDataQueue.Dequeue());
                }

                serverSocket.EnqueueData(messageData);

                if (!serverSocketStarted)
                {
                    serverSocket.Start();
                }
            }
        }

        private void ResetCurrentMessage()
        {
            this.currentMessage = new HttpMessage(this.Settings);
        }

        private void EnsureClientMessage()
        {
            if (this.currentMessage == null)
            {
                this.ResetCurrentMessage();
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