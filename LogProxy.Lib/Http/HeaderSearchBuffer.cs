using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogProxy.Lib.Http
{
    internal class HeaderSearchBuffer
    {
        private const string HeaderLinesSeparator = "\r\n";
        private const string HeaderValueSeparator = ":";
        private const string HeaderStatusLineValuesSeparator = " ";
        private const string ContentLengthHeader = "CONTENT-LENGTH";

        private static readonly int headerBufferSize = 4096;
        private static readonly byte[] headerDelimiter = new byte[] { 13, 10, 13, 10 };

        private byte[] headerBuffer;
        private int currentHeaderBufferSize;
        private int nextSearchStart;
        private HeaderSearchBufferType headerSearchBufferType;

        public HeaderSearchBuffer(HeaderSearchBufferType headerSearchBufferType)
        {
            this.headerBuffer = new byte[headerBufferSize];
            this.headerSearchBufferType = headerSearchBufferType;
        }

        public static int HeaderDelimiterSize
        {
            get 
            {
                return headerDelimiter.Length;
            }
        }

        public byte[] ProcessedData { get; private set; }

        public string HeaderContent { get; private set; }

        public HttpHeadersSummary HeadersSummary { get; private set; }

        public int ContentOffset { get; private set; }

        public bool AddDataAndCheckHeadersFound(byte[] data)
        {
            if (this.HeaderContent == null)
            {
                int newHeaderBufferSize = this.currentHeaderBufferSize + data.Length;

                Utils.EnsureArraySize(ref this.headerBuffer, newHeaderBufferSize, this.currentHeaderBufferSize);
                Buffer.BlockCopy(data, 0, this.headerBuffer, this.currentHeaderBufferSize, data.Length);

                this.currentHeaderBufferSize = newHeaderBufferSize;

                return this.FindAndProcessHeaders();
            }
            else
            {
                return false;
            }
        }

        private bool FindAndProcessHeaders()
        {
            if (this.nextSearchStart >= this.currentHeaderBufferSize)
            {
                return false;
            }

            byte firstDelimiterByte = headerDelimiter[0];

            while (true)
            {
                int searchStart = this.nextSearchStart;
                int delimiterStartIndex = Array.IndexOf(
                    array: this.headerBuffer,
                    value: firstDelimiterByte,
                    startIndex: searchStart,
                    count: this.currentHeaderBufferSize - searchStart);

                if (delimiterStartIndex < 0)
                {
                    this.nextSearchStart = this.currentHeaderBufferSize;
                    return false;
                }

                if (this.currentHeaderBufferSize < (delimiterStartIndex + headerDelimiter.Length))
                {
                    this.nextSearchStart = delimiterStartIndex;
                    return false;
                }

                if (!Utils.ArraysEqual(this.headerBuffer, delimiterStartIndex, headerDelimiter, 0, headerDelimiter.Length))
                {
                    this.nextSearchStart = delimiterStartIndex + 1;
                    continue;
                }

                string headers = Encoding.ASCII.GetString(headerBuffer, 0, delimiterStartIndex);
                this.ProcessReadyHeaders(headers);
                
                return true;
            }
        }

        private void ProcessReadyHeaders(string headers)
        {
            int contentOffsetSize = this.currentHeaderBufferSize - headers.Length - HeaderDelimiterSize;
            byte[] contentOffsetData = null;
            if (contentOffsetSize > 0)
            {
                contentOffsetData = new byte[contentOffsetSize];
                Buffer.BlockCopy(this.headerBuffer, headers.Length + HeaderDelimiterSize, contentOffsetData, 0, contentOffsetSize);
            }

            HttpHeadersSummary summary = new HttpHeadersSummary();

            string[] headerLines = headers.SplitNoEmpty(HeaderLinesSeparator);

            if (headerLines.Length < 1)
            {
                throw new InvalidOperationException("HTTP message should contain at least one status line");
            }

            summary.StatusLine = this.ProcessHeaderStatusLine(headerLines[0]);

            summary.Headers = headerLines
                .Skip(1)
                .Select(line => ProcessHeaderLine(line))
                .ToLookup(lineParts => lineParts[0], lineParts => lineParts[1]);

            string contentLengthValue = summary.Headers[ContentLengthHeader].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(contentLengthValue))
            {
                summary.ContentLength = contentLengthValue.ToInt(errorMessage: "Content length header value invalid");
            }

            headers = string.Join(HeaderStatusLineValuesSeparator, summary.StatusLine)
                + HeaderLinesSeparator 
                + string.Join(HeaderLinesSeparator, headerLines.Skip(1));

            this.ProcessedData = System.Text.Encoding.ASCII.GetBytes(headers)
                .Concat(headerDelimiter)
                .Concat(contentOffsetData ?? new byte[0])
                .ToArray();

            summary.HeaderLength = headers.Length + headerDelimiter.Length;
            summary.HeadersContent = headers;

            this.HeaderContent = headers;
            this.ContentOffset = contentOffsetSize;
            this.HeadersSummary = summary;
        }

        private IList<string> ProcessHeaderStatusLine(string statusLine)
        {
            string[] statusLineParts = statusLine.SplitNoEmpty(HeaderStatusLineValuesSeparator, max: 3);

            if (statusLineParts.Length > 1 && this.headerSearchBufferType == HeaderSearchBufferType.Request)
            {
                string requestUri = statusLineParts[1];

                // some servers don't understand absolute paths in HTTP request status line
                // (browser make it absolute in requests to proxies):

                int searchStart = "https://".Length;
                if (requestUri != null && !requestUri.StartsWith("/") && requestUri.Length > searchStart)
                {
                    int relativeOffset = requestUri.IndexOf('/', searchStart);
                    if (relativeOffset > 0)
                    {
                        statusLineParts[1] = requestUri.Substring(relativeOffset);
                    }
                }
            }

            return statusLineParts.ToList();
        }

        private static string[] ProcessHeaderLine(string headerLine)
        {
            string[] headerLineParts = headerLine.SplitNoEmpty(HeaderValueSeparator, max: 2);

            if (headerLineParts.Length != 2)
            {
                throw new InvalidOperationException("Header should consist of name/value pair");
            }

            headerLineParts[0] = headerLineParts[0].ToUpperInvariant();
            headerLineParts[1] = headerLineParts[1].Trim();

            return headerLineParts;
        }

        public void Reset()
        { 
            this.currentHeaderBufferSize = 0;
            this.nextSearchStart = 0;
        }
    }
}
