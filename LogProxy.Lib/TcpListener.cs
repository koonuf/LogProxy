using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LogProxy.Lib.Sockets;

namespace LogProxy.Lib
{
    public class TcpListener : IDisposable
    {
        private const int ConnectionQueueLength = 64;

        private Socket listenSocket;
        private AutoResetEvent connectionWaitHandle;
        private ProxySettings settings;

        public TcpListener(ProxySettings settings)
        {
            this.settings = settings;
        }

        public void ListenToNewConnections()
        {
            if (this.listenSocket == null)
            {
                if (this.settings.CertificateProvider != null)
                {
                    this.settings.CertificateProvider.EnsureRootCertificate();
                }

                this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(new IPEndPoint(IPAddress.Any, this.settings.ListenPort));
                listenSocket.Listen(ConnectionQueueLength);

                this.connectionWaitHandle = new AutoResetEvent(false);

                while (true)
                {
                    listenSocket.BeginAccept(HandleAsyncConnection, listenSocket);
                    connectionWaitHandle.WaitOne();
                }
            }
        }

        private void HandleAsyncConnection(IAsyncResult result)
        {
            connectionWaitHandle.Set();

            Socket listener = (Socket)result.AsyncState;
            Socket workerSocket = listener.EndAccept(result);

            var proxy = new ClientWorker(workerSocket, this.settings);
            proxy.StartRelay();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.listenSocket != null)
            {
                this.listenSocket.Dispose();
            }

            if (this.connectionWaitHandle != null)
            {
                this.connectionWaitHandle.Dispose();
            }
        }
    }
}
