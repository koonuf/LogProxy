using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using LogProxy.Lib;
using LogProxy.Lib.Inspection;

namespace LogProxy.Inspectors
{
    public class LoggingMessageInspector : IMessageInspector
    {
        private const string LoggerDateFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";

        private ProxySettings settings;
        private Stopwatch watch;
        private FileStream requestLogger;
        private FileStream responseLogger;
        private string fileNamePrefix;

        public LoggingMessageInspector(ProxySettings settings)
        {
            this.settings = settings;
        }

        public void ClientReceiveStart()
        {
            throw new System.NotImplementedException();
        }

        public void AddRequestData(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public void AddResponseData(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public void RequestHeadersParsed(Lib.HttpHeadersSummary headers)
        {
            throw new System.NotImplementedException();
        }

        public void ResponseHeadersParsed(Lib.HttpHeadersSummary headers)
        {
            throw new System.NotImplementedException();
        }

        public void ServerReceiveFinished()
        {
            throw new System.NotImplementedException();
        }

        private void InitLoggers()
        {
            string now = DateTime.Now.ToString(LoggerDateFormat, CultureInfo.InvariantCulture);
            string guid = Guid.NewGuid().ToString();

            this.fileNamePrefix = now + "-" + guid;

            if (this.watch == null)
            {
                this.watch = Stopwatch.StartNew();
                this.RequestStartTime = DateTime.Now;
                if (this.settings.LogMessageBody)
                {
                    this.InitLoggers();
                }
            }
        }

        public DateTime RequestStartTime { get; private set; }

        private void EnsureRequestLogger()
        {
            if (this.settings.LogMessageBody && this.requestLogger == null)
            {
                string folder = this.settings.MessageBodyLogDirectory;
                string requestFileName = this.fileNamePrefix + "-request.txt";
                this.requestLogger = new FileStream(Path.Combine(folder, requestFileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1024 * 20);
            }
        }

        private void EnsureResponseLogger()
        {
            if (this.settings.LogMessageBody && this.responseLogger == null)
            {
                string folder = this.settings.MessageBodyLogDirectory;
                string responseFileName = this.fileNamePrefix + "-response.txt";
                this.responseLogger = new FileStream(Path.Combine(folder, responseFileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1024 * 20);
            }
        }

        private static void LogData(FileStream logger, byte[] data)
        {
            if (logger != null)
            {
                try
                {
                    logger.Write(data, 0, data.Length);
                    logger.Flush();
                }
                catch (ObjectDisposedException)
                { }
            }
        }

        public string SoapAction
        {
            get
            {
                if (this.soapAction != null)
                {
                    return this.soapAction;
                }

                if (this.soapActionSearchBuffer != null)
                {
                    this.soapActionSearchBuffer.Wait();
                    return this.soapActionSearchBuffer.SoapAction;
                }

                return null;
            }
        }

        private string soapAction;
        private SoapActionSearchBuffer soapActionSearchBuffer;

        public void AddContent(byte[] content)
        {
            if (string.IsNullOrEmpty(this.soapAction))
            {
                this.EnsureSoapActionBuffer(null);
                this.soapActionSearchBuffer.AddContent(content, 0, content.Length);
            }
        }

        private void EnsureSoapActionBuffer(byte[] bufferData)
        {
            if (this.soapActionSearchBuffer == null)
            {
                this.soapActionSearchBuffer = new SoapActionSearchBuffer();
                if (bufferData != null)
                {
                    this.soapActionSearchBuffer.AddContent(bufferData, 0, bufferData.Length);
                }

                this.soapActionSearchBuffer.StartSearch();
            }
        }

        public void Finish()
        {
            if (this.soapActionSearchBuffer != null)
            {
                this.soapActionSearchBuffer.FinishContentTransfer();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.requestLogger != null)
            {
                this.requestLogger.Dispose();
                this.requestLogger = null;
            }

            if (this.responseLogger != null)
            {
                this.responseLogger.Dispose();
                this.responseLogger = null;
            }
        }
    }
}
