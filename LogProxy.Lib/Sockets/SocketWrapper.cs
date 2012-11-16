using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using LogProxy.Lib.Streams;

namespace LogProxy.Lib.Sockets
{
    public class SocketWrapper : IDisposable
    {
        private const string SecureTunnelEstablishedResponse = "HTTP/1.1 200 Connection established\r\n\r\n";

        private Socket workerSocket;
        private SslStream sslStream;

        private volatile bool isSecure;
        private volatile bool inTransferToSecure;
        private ManualResetEventSlim transferToSecureWaitHandle;
        private ProxySettings settings;

        public SocketWrapper(Socket workerSocket, ProxySettings settings)
        {
            this.workerSocket = workerSocket;
            this.settings = settings;
        }

        public void BeginReceive(byte[] buffer, int offset, int size, AsyncCallback callback)
        {
            if (this.inTransferToSecure)
            {
                this.transferToSecureWaitHandle.Wait();
            }

            if (this.isSecure)
            {
                this.sslStream.BeginRead(buffer, offset, size, callback, asyncState: null);
            }
            else
            {
                this.workerSocket.BeginReceive(buffer, offset, size, SocketFlags.None, callback, state: null);
            }
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            if (this.isSecure)
            {
                return this.sslStream.EndRead(asyncResult);
            }
            else
            {
                return this.workerSocket.EndReceive(asyncResult);
            }
        }

        public void Send(byte[] buffer)
        {
            if (this.isSecure)
            {
                this.sslStream.Write(buffer);
            }
            else
            {
                this.workerSocket.Send(buffer);
            }
        }

        public void TransferToSecureAsServer(string host)
        {
            if (!this.isSecure)
            {
                this.sslStream = new SslStream(new SocketStream(this.workerSocket), leaveInnerStreamOpen: true);
                this.sslStream.AuthenticateAsClient(host);
                this.isSecure = true;
            }
        }

        public void StartTransferToSecureAsClient()
        {
            if (!this.isSecure)
            {
                this.isSecure = true;
                this.transferToSecureWaitHandle = new ManualResetEventSlim(initialState: false);
                this.inTransferToSecure = true;
            }
        }

        public bool IsSecure
        {
            get { return this.isSecure; }
        }

        public void EndTransferToSecureAsClient(string remoteHost)
        {
            if (this.inTransferToSecure)
            {
                this.inTransferToSecure = false;

                this.workerSocket.Send(Encoding.ASCII.GetBytes(SecureTunnelEstablishedResponse));

                this.sslStream = new SslStream(new SocketStream(this.workerSocket), leaveInnerStreamOpen: true);
                X509Certificate2 serverCertificate = this.settings.CertificateProvider.GetCertificateForHost(remoteHost);
                this.sslStream.AuthenticateAsServer(serverCertificate);

                this.transferToSecureWaitHandle.Set();
            }
        }

        public void Close()
        {
            if (this.sslStream != null)
            {
                this.sslStream.Close();
            }

            Utils.CloseSocket(this.workerSocket);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Utils.DisposeSocket(this.workerSocket);

            if (this.sslStream != null)
            {
                this.sslStream.Dispose();
            }

            if (this.transferToSecureWaitHandle != null)
            {
                this.transferToSecureWaitHandle.Dispose();
            }
        }
    }
}
