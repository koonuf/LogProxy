using System;
using System.Text;
 
namespace LogProxy.Lib.Http
{
    /// <summary>
    /// Responsible for traversing and delimiting HTTP responses, which are being transferred in 
    /// chunked mode (chunked transfer encoding)
    /// </summary>
    public class ChunkSearchBuffer
    {
        private const int FinalChunkLength = 1 + 2 + 2;

        private static readonly int contentBufferArraySize = 1024 * 8;
        private static readonly byte[] chunkSizeDelimiter = new byte[] { 13, 10 };

        private byte[] contentBuffer;
        private int contentLoadedSize;
        private int contentBufferSize;

        private int? currentChunkSize;
        private int delimiterStartIndex;
        private bool loadingTrailer;

        private HeaderSearchBuffer headerSearchBuffer;

        public bool FinishedLoading { get; private set; }

        public int ContentOffset { get; private set; }

        public ChunkSearchBuffer()
        {
            this.contentBuffer = new byte[contentBufferArraySize];
        }

        public void AddContentData(byte[] data)
        {
            if (this.FinishedLoading || data.Length == 0)
            {
                return;
            }

            if (this.loadingTrailer)
            {
                this.LoadTrailer(data);
                return;
            }

            this.AddToContentBuffer(data);

            while (true)
            {
                if (this.currentChunkSize.HasValue)
                {
                    if (this.contentLoadedSize < this.currentChunkSize.Value)
                    {
                        return;
                    }
                    else
                    {
                        if (this.currentChunkSize.Value == FinalChunkLength)
                        {
                            this.CheckOnFinalChunk();
                            return;
                        }
                        
                        this.ResetContentBuffer();
                    }
                }

                if (!this.FindChunkDelimiter())
                {
                    return;
                }

                string chunkSizeHeader = this.GetChunkSizeHeader();
                int chunkHeaderLength = chunkSizeHeader.Length + chunkSizeDelimiter.Length;

                this.currentChunkSize = Convert.ToInt32(chunkSizeHeader, 16) + chunkHeaderLength + 2;

                continue;
            }
        }

        private void LoadTrailer(byte[] data)
        {
            this.headerSearchBuffer.AddData(data);
            string headers;
            if (this.headerSearchBuffer.FindHeaders(out headers))
            {
                this.FinishedLoading = true;
                this.ContentOffset = this.headerSearchBuffer.ContentOffset;
            }
        }

        private void CheckOnFinalChunk()
        {
            if (this.contentBuffer[3] == chunkSizeDelimiter[0])
            {
                // no trailer
                this.FinishedLoading = true;
                this.ContentOffset = this.contentLoadedSize - FinalChunkLength;
            }
            else
            {
                this.headerSearchBuffer = new HeaderSearchBuffer();
                this.loadingTrailer = true;

                int offsetStart = FinalChunkLength - 2;
                byte[] offsetData = Utils.CopyArray(this.contentBuffer, offsetStart, this.contentBufferSize - offsetStart);
                this.LoadTrailer(offsetData);
            }
        }

        private void ResetContentBuffer()
        {
            int offset = this.contentLoadedSize - this.currentChunkSize.Value;
            if (offset > 0)
            {
                Buffer.BlockCopy(
                    src: this.contentBuffer,
                    srcOffset: this.contentBufferSize - offset, 
                    dst: this.contentBuffer, 
                    dstOffset: 0,
                    count: offset); 
            }

            this.contentBufferSize = offset;
            this.contentLoadedSize = offset;
            this.currentChunkSize = null;
        }

        private string GetChunkSizeHeader()
        {
            return Encoding.ASCII.GetString(this.contentBuffer, 0, this.delimiterStartIndex);
        }

        private bool FindChunkDelimiter()
        {
            if (this.contentBufferSize < 3)
            {
                return false;
            }

            this.delimiterStartIndex = Array.IndexOf(
                        array: this.contentBuffer,
                        value: chunkSizeDelimiter[0],
                        startIndex: 0,
                        count: this.contentBufferSize);

            if (this.delimiterStartIndex < 0)
            {
                return false;
            }

            if (this.contentBufferSize < (this.delimiterStartIndex + chunkSizeDelimiter.Length))
            {
                return false;
            }

            if (this.contentBuffer[this.delimiterStartIndex + 1] != chunkSizeDelimiter[1])
            {
                throw new InvalidOperationException("Delimiter byte in chunk header");
            }

            return true;
        }

        private void AddToContentBuffer(byte[] data)
        {
            int newContentLoadedSize = this.contentLoadedSize + data.Length;

            if (!this.currentChunkSize.HasValue 
                || this.currentChunkSize.Value < newContentLoadedSize
                || (this.currentChunkSize.Value == FinalChunkLength && this.currentChunkSize.Value == newContentLoadedSize))
            {
                int newContentBufferSize = this.contentBufferSize + data.Length;
                Utils.EnsureArraySize(ref this.contentBuffer, newContentBufferSize, this.contentBufferSize);
                Buffer.BlockCopy(data, 0, this.contentBuffer, this.contentBufferSize, data.Length);
                this.contentBufferSize = newContentBufferSize;
            }

            this.contentLoadedSize = newContentLoadedSize;
        }
    }
}