using System;
using LogProxy.Lib.Inspection;

namespace LogProxy.Lib.Http
{
    /// <summary>
    /// HTTP request and corresponding response from server
    /// </summary>
    public class HttpMessage
    {
        private HeaderSearchBuffer requestHeaderBuffer;
        private HeaderSearchBuffer responseHeaderBuffer;
        private ProxySettings settings;
        private IHttpMessageInspector messageInspector;

        public HttpMessage(ProxySettings settings)
        {
            this.settings = settings;

            this.Request = new HttpRequestInfo();
            this.Response = new HttpResponseInfo();

            this.requestHeaderBuffer = new HeaderSearchBuffer();
            this.responseHeaderBuffer = new HeaderSearchBuffer();

            if (settings.InspectorFactory != null)
            {
                this.messageInspector = settings.InspectorFactory.CreateHttpMessageInspector(this);
            }
        }

        public HttpRequestInfo Request { get; set; }

        public HttpResponseInfo Response { get; set; }

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
                    this.messageInspector.SafeRequestHeadersParsed(headersSummary);
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

            this.messageInspector.SafeAddRequestData(messageData);

            return messageData;
        }

        public byte[] AddResponseData(byte[] data)
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
                    this.messageInspector.SafeResponseHeadersParsed(headersSummary);
                }
            }

            byte[] messageData;

            int loadedExtraContentOffset = this.Response.ExtraContentOffset;
            if (loadedExtraContentOffset > 0)
            {
                messageData = Utils.CopyArray(data, 0, data.Length - loadedExtraContentOffset);
            }
            else
            {
                messageData = data;
            }

            this.messageInspector.SafeAddResponseData(messageData);

            return messageData;
        }

        public void Stop()
        {
            this.messageInspector.SafeServerReceiveFinished();
        }
    }
}
