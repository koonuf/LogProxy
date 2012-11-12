using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogProxy.Lib
{
    internal class HeaderSearchBuffer
    {
        private const string HeaderLinesSeparator = "\r\n";
        private const string HeaderValueSeparator = ":";
        private const string HeaderStatusLineValuesSeparator = " ";
        private const string ContentLengthHeader = "CONTENT-LENGTH";

        private static readonly int headerBufferSize = 4096;
        private static readonly byte[] headerDelimiter = new byte[] { 13, 10, 13, 10 };

        public byte[] headerBuffer;
        public int currentHeaderBufferSize;
        public int nextSearchStart;

        public HeaderSearchBuffer()
        {
            this.headerBuffer = new byte[headerBufferSize];
        }

        public static int HeaderDelimiterSize
        {
            get 
            {
                return headerDelimiter.Length;
            }
        }

        public void AddData(byte[] data)
        {
            int newHeaderBufferSize = this.currentHeaderBufferSize + data.Length;

            Utils.EnsureArraySize(ref this.headerBuffer, newHeaderBufferSize, this.currentHeaderBufferSize);
            Buffer.BlockCopy(data, 0, this.headerBuffer, this.currentHeaderBufferSize, data.Length);

            currentHeaderBufferSize = newHeaderBufferSize;
        }

        public int ContentOffset { get; private set; }

        public bool FindHeaders(out string headers)
        {
            headers = null;

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

                headers = Encoding.ASCII.GetString(headerBuffer, 0, delimiterStartIndex);
                this.ContentOffset = this.currentHeaderBufferSize - delimiterStartIndex - headerDelimiter.Length;
                return true;
            }
        }

        public bool FindHeaders(out HttpHeadersSummary headersSummary)
        {
            headersSummary = null;
            string headersContent;

            if (this.FindHeaders(out headersContent))
            {
                headersSummary = ParseHeaders(headersContent);
                return true;
            }

            return false;
        }

        private static HttpHeadersSummary ParseHeaders(string headers)
        {
            HttpHeadersSummary summary = new HttpHeadersSummary { HeadersContent = headers };

            string[] headerParts = headers.SplitNoEmpty(HeaderLinesSeparator);

            if (headerParts.Length < 1)
            {
                throw new InvalidOperationException("HTTP message should contain at least one status line");
            }

            summary.HeaderLength = headers.Length + headerDelimiter.Length;
            summary.StatusLine = headerParts[0].SplitNoEmpty(HeaderStatusLineValuesSeparator, max: 3).ToList();

            summary.Headers = headerParts
                .Skip(1)
                .Select(line => CheckHeaderValue(line.SplitNoEmpty(HeaderValueSeparator, max: 2)))
                .ToLookup(h => h[0].ToUpperInvariant(), h => h[1].Trim());

            string contentLengthValue = summary.Headers[ContentLengthHeader].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(contentLengthValue))
            {
                summary.ContentLength = contentLengthValue.ToInt(errorMessage: "Content length header value invalid");
            }

            return summary;
        }

        private static string[] CheckHeaderValue(string[] value)
        {
            if (value.Length != 2)
            {
                throw new InvalidOperationException("Header should consist of name/value pair");
            }

            return value;
        }

        public void Reset()
        { 
            this.currentHeaderBufferSize = 0;
            this.nextSearchStart = 0;
        }
    }
}
