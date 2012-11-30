using System;
using System.Globalization;
using System.IO;

namespace LogProxy.Lib.Inspection.Implementation
{
    public class LoggingMessageInspector : IHttpMessageInspector
    {
        private const string LoggerDateFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";

        private ProxySettings settings;
        private FileStream requestLogger;
        private FileStream responseLogger;
        private string fileNamePrefix;

        public LoggingMessageInspector(ProxySettings settings)
        {
            this.settings = settings;
        }

        public void AddRequestData(byte[] data)
        {
            if (this.settings.LogMessageBody)
            {
                this.EnsureRequestLogger();
                LogData(this.requestLogger, data);
            }
        }

        public void AddResponseData(byte[] data)
        {
            if (this.settings.LogMessageBody)
            {
                this.EnsureResponseLogger();
                LogData(this.responseLogger, data);
            }
        }

        public void RequestHeadersParsed(Lib.Http.HttpHeadersSummary headers)
        {
        }

        public void ResponseHeadersParsed(Lib.Http.HttpHeadersSummary headers)
        {
        }

        public void ServerReceiveFinished()
        {
        }

        private void EnsureFileNamePrefix()
        {
            if (this.fileNamePrefix == null)
            {
                string now = DateTime.Now.ToString(LoggerDateFormat, CultureInfo.InvariantCulture);
                string guid = Guid.NewGuid().ToString();

                this.fileNamePrefix = now + "-" + guid;
            }
        }

        private void EnsureRequestLogger()
        {
            if (this.requestLogger == null && this.settings.LogMessageBody)
            {
                this.EnsureFileNamePrefix();

                string folder = this.settings.MessageBodyLogDirectory;
                string requestFileName = this.fileNamePrefix + "-request.txt";
                this.requestLogger = new FileStream(Path.Combine(folder, requestFileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1024 * 20);
            }
        }

        private void EnsureResponseLogger()
        {
            if (this.responseLogger == null && this.settings.LogMessageBody)
            {
                this.EnsureFileNamePrefix();

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
                }
                catch (ObjectDisposedException)
                { }
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
