using System;
using System.Linq;

namespace LogProxy.Lib.Http
{
    public class HttpRequestInfo
    {
        private const string HostHeader = "HOST";
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

        public bool FinishedLoading { get; private set; }

        public int ExtraContentOffset { get; private set; }

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

            this.IsInitialized = true;

            // Recalculate LoadedContentLength and ExtraContentOffset
            this.IncrementContentSize(0);
        }
    }
}