using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace LogProxy.Lib
{
    public class HttpMessage : IDisposable
    {
        private HeaderSearchBuffer requestHeaderBuffer;
        private HeaderSearchBuffer responseHeaderBuffer;
        private ProxySettings settings;

        public HttpMessage(ProxySettings settings)
        {
            this.Request = new HttpRequestInfo();
            this.Response = new HttpResponseInfo();
            this.settings = settings;

            this.requestHeaderBuffer = new HeaderSearchBuffer();
            this.responseHeaderBuffer = new HeaderSearchBuffer();
        }

        public bool ServerRelayInitiated { get; set; }

        public byte[] AddRequestData(byte[] data)
        {
            this.Request.IncrementContentSize(data.Length);

            if (!this.Request.IsInitialized)
            {
                this.requestHeaderBuffer.AddData(data);

                HttpHeadersSummary headersSummary = null;
                if (this.requestHeaderBuffer.FindHeaders(out headersSummary))
                {
                    this.Request.InitFromSummary(headersSummary);
                    this.requestHeaderBuffer.Reset();
                }
            }

            byte[] messageData;

            int loadedExtraContentOffset = this.Request.ExtraContentOffset;
            if (loadedExtraContentOffset > 0)
            {
                messageData = Utils.CopyArray(data, 0, data.Length - loadedExtraContentOffset);
            }
            else
            {
                messageData = data;
            }

            return messageData;
        }

        public void AddResponseData(byte[] data)
        {
            this.Response.AddContent(data);

            if (!this.Response.IsInitialized)
            {
                this.responseHeaderBuffer.AddData(data);

                HttpHeadersSummary headersSummary;
                if (this.responseHeaderBuffer.FindHeaders(out headersSummary))
                {
                    this.Response.InitFromSummary(headersSummary, this);
                    this.responseHeaderBuffer.Reset();
                }
            }
        }

        public HttpRequestInfo Request { get; set; }

        public HttpResponseInfo Response { get; set; }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
