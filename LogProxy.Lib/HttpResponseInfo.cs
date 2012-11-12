using System;
using System.Collections.Generic;
using System.Linq;

namespace LogProxy.Lib
{
    public class HttpResponseInfo
    {
        public const string ContinueStatusCode = "100";
        public const string TransferEncodingHeaderName = "TRANSFER-ENCODING";
        public const string ChunkedTransferValue = "CHUNKED";

        private ChunkSearchBuffer chunkSearchBuffer;
        private byte[] contentBuffer = new byte[1024 * 4];

        public int? ContentLength { get; set; }

        public int HeaderLength { get; set; }

        public int LoadedContentLength { get; private set; }

        public string HttpVersion { get; set; }

        public string Status { get; set; }

        public string Reason { get; set; }

        public ILookup<string, string> Headers { get; set; }

        public bool IsContinueResponse 
        {
            get 
            {
                return this.Status == ContinueStatusCode;
            }
        }

        public bool IsInitialized { get; private set; }

        private void AddToContentBuffer(byte[] content)
        {
            int newSize = this.LoadedContentLength + content.Length;
            Utils.EnsureArraySize(ref this.contentBuffer, newSize, this.LoadedContentLength);
            Buffer.BlockCopy(content, 0, this.contentBuffer, this.LoadedContentLength, content.Length);
        }

        public void AddContent(byte[] content)
        {
            if (!this.IsInitialized)
            {
                this.AddToContentBuffer(content);
                this.LoadedContentLength += content.Length;
            }
            else
            {
                if (this.IsChunkedTransfer)
                {
                    this.EnsureChunkSearchBuffer();

                    this.chunkSearchBuffer.AddContentData(content);
                    this.ExtraContentOffset = this.chunkSearchBuffer.ContentOffset;
                }
                else
                {
                    int fullSize = this.HeaderLength + this.ContentLength.GetValueOrDefault(0);
                    this.LoadedContentLength += content.Length;
                    if (this.LoadedContentLength >= fullSize)
                    {
                        this.ExtraContentOffset = this.LoadedContentLength - fullSize;
                        this.LoadedContentLength = fullSize;
                    }
                }
            }
        }

        private byte[] GetContentBufferData()
        {
            int contentOffsetSize = this.LoadedContentLength - this.HeaderLength;
            if (contentOffsetSize > 0)
            {
                byte[] contentOffset = Utils.CopyArray(this.contentBuffer, this.LoadedContentLength - contentOffsetSize, contentOffsetSize);
                return contentOffset;
            }

            return null;
        }

        private void EnsureChunkSearchBuffer()
        {
            if (this.chunkSearchBuffer == null)
            {
                this.chunkSearchBuffer = new ChunkSearchBuffer();
                var bufferData = this.GetContentBufferData();
                if (bufferData != null)
                {
                    this.chunkSearchBuffer.AddContentData(bufferData);
                }
            }
        }

        public bool FinishedLoading
        {
            get
            {
                if (this.IsContinueResponse)
                {
                    return true;
                }

                if (this.chunkSearchBuffer != null)
                {
                    return this.chunkSearchBuffer.FinishedLoading;
                }

                return this.IsInitialized && this.LoadedContentLength >= (this.HeaderLength + this.ContentLength.GetValueOrDefault(0));
            }
        }

        public int ExtraContentOffset { get; private set; }

        public bool IsChunkedTransfer { get; private set; }

        public void InitFromSummary(HttpHeadersSummary summary, HttpMessage httpMessage)
        {
            if (summary.StatusLine.Count != 3)
            {
                throw new InvalidOperationException("First line of HTTP request should consist of 3 parts");
            }

            this.ContentLength = summary.ContentLength.GetValueOrDefault(0);
            this.HttpVersion = summary.StatusLine[0];
            this.Status = summary.StatusLine[1];
            this.Reason = summary.StatusLine[2];
            this.Headers = summary.Headers;
            this.HeaderLength = summary.HeaderLength;

            var transferEncodingValue = this.Headers[TransferEncodingHeaderName].FirstOrDefault();
            if (transferEncodingValue != null && transferEncodingValue.ToUpperInvariant() == ChunkedTransferValue)
            {
                this.IsChunkedTransfer = true;
            }

            this.IsInitialized = true;

            // Recalculate LoadedContentLength and ExtraContentOffset
            this.AddContent(new byte[0]);
        }
    }
}
