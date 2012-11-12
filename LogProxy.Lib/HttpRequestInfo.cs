using System;
using System.Collections.Generic;
using System.Linq;

namespace LogProxy.Lib
{
    public class HttpRequestInfo
    {
        private const string HostHeader = "HOST";
        private const string ContentTypeHeaderName = "CONTENT-TYPE";
        private const string SoapContentTypeValue = "APPLICATION/SOAP+XML";
        private const string SoapActionHeader = "SOAPACTION";
        private const string ConnectHttpMethod = "CONNECT";

        public string Host { get; set; }

        public string Method { get; set; }

        public string Location { get; set; }

        public string HttpVersion { get; set; }

        public ILookup<string, string> Headers { get; set; }

        public int ContentLength { get; set; }

        public int HeaderLength { get; set; }

        public int LoadedContentLength { get; private set; }

        public void IncrementContentSize(int contentSize)
        {
            int newSize = this.LoadedContentLength + contentSize;

            if (this.IsInitialized)
            {
                int fullSize = this.HeaderLength + this.ContentLength;

                if (newSize >= fullSize)
                {
                    this.FinishedLoading = true;
                    this.ExtraContentOffset = newSize - fullSize;
                    newSize = fullSize;
                }
            }

            this.LoadedContentLength = newSize;
        }

        public bool IsInitialized { get; set; }

        public string SoapAction { get; private set; }

        public bool FinishedLoading { get; private set; }

        public int ExtraContentOffset { get; private set; }

        public bool IsSoapMessage { get; set; }

        public bool IsRequestToSecureConnection
        {
            get 
            {
                return this.IsInitialized && this.Method.Equals(ConnectHttpMethod, StringComparison.OrdinalIgnoreCase);
            }
        }

        public void InitFromSummary(HttpHeadersSummary summary)
        {
            if (summary.StatusLine.Count != 3)
            {
                throw new InvalidOperationException("First line of HTTP request should consist of 3 parts");
            }

            this.ContentLength = summary.ContentLength.GetValueOrDefault(0);
            this.Host = summary.Headers.SafeGetValue(HostHeader, errorMessage: "Host header not found in request");
            this.Method = summary.StatusLine[0];
            this.Location = summary.StatusLine[1];
            this.HttpVersion = summary.StatusLine[2];
            this.Headers = summary.Headers;
            this.HeaderLength = summary.HeaderLength;

            string contentType = this.Headers[ContentTypeHeaderName].FirstOrDefault();
            string soapAction = this.Headers[SoapActionHeader].FirstOrDefault();

            if (soapAction != null ||
                (contentType != null && contentType.ToUpperInvariant().Contains(SoapContentTypeValue)))
            {
                this.IsSoapMessage = true;
                this.SoapAction = soapAction;
            }

            this.IsInitialized = true;

            // Recalculate LoadedContentLength and ExtraContentOffset
            this.IncrementContentSize(0);
        }
    }
}