using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using LogProxy.Lib.Inspection;
using LogProxy.Lib.Logging;

namespace LogProxy.Lib
{
    public class HostConnector
    {
        private Action<Socket> connectionMadeCallback; 
        private Action errorCallback;
        private int port;
        private string host;
        private IServerConnectionInspector connectionInspector;
        private bool hostIsIP;

        private static ConcurrentDictionary<string, IPAddress> dnsCache = new ConcurrentDictionary<string, IPAddress>();

        public static IDictionary<string, IPAddress> DnsCache
        {
            get 
            {
                return dnsCache;
            }
        }

        public HostConnector(string host, bool secure, Action<Socket> connectionMadeCallback, Action errorCallback, IInspectorFactory inspectorFactory = null)
        {
            this.connectionMadeCallback = connectionMadeCallback;
            this.errorCallback = errorCallback;

            Uri uri = new Uri((secure ? "https://" : "http://") + host);
            this.host = uri.Host;
            this.port = uri.Port;
            this.hostIsIP = uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6;

            if (inspectorFactory != null)
            {
                this.connectionInspector = inspectorFactory.CreateServerConnectionInspector(this.host, this.port, secure);
            }
        }

        public void StartConnecting()
        {
            this.connectionInspector.SignalStartConnection();

            if (this.hostIsIP)
            {
                this.ConnectToIP(IPAddress.Parse(this.host));
            }
            else
            {
                string hostKey = this.host.ToUpperInvariant();

                IPAddress ip;
                if (dnsCache.TryGetValue(hostKey, out ip))
                {
                    this.ConnectToIP(ip);
                }
                else
                {
                    this.connectionInspector.SignalStartDnsResolve();

                    try
                    {
                        Dns.BeginGetHostAddresses(this.host, this.HostEntryReceived, state: hostKey);
                    }
                    catch (SocketException)
                    {
                        this.OnError();
                    }
                }
            }
        }

        private void OnError(Socket socket = null)
        {
            this.errorCallback();

            if (socket != null)
            {
                socket.Dispose();
            }
        }

        private void HostEntryReceived(IAsyncResult result)
        {
            IPAddress ip;

            try
            {
                var ips = Dns.EndGetHostAddresses(result);
                this.connectionInspector.SignalFinishDnsResolve();

                ip = ips[0];
                dnsCache[(string)result.AsyncState] = ip;
            }
            catch (SocketException)
            {
                this.OnError();
                return;
            }

            this.ConnectToIP(ip);
        }

        private void ConnectToIP(IPAddress ipAddress)
        {
            IPEndPoint endpoint = new IPEndPoint(ipAddress, this.port);

            Socket socket = null;
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.BeginConnect(endpoint, r =>
                {
                    try
                    {
                        socket.EndConnect(r);
                    }
                    catch (SocketException)
                    {
                        this.OnError(socket);
                        return;
                    }

                    this.connectionInspector.SignalConnectionMade();
                    this.connectionMadeCallback(socket);
                }, state: null);
            }
            catch (SocketException)
            {
                this.OnError(socket);
            }
        }
    }
}
