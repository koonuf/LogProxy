using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Inspection
{
    public class SoapCsvMessageLogger
    {
        private const string MessagePartSeparator = ";";
        private const string MessageSeparator = "\r\n";
        private const string LogTimerFormat = "dd.MM.yyyy HH:mm:ss";

        private readonly object syncLock = new object();

        private FileStream logStream;
        private Timer inactivityTimer;
        private TimeSpan inactivityLogTime;

        private BlockingCollection<HttpMessage> finishedMessagesQueue = new BlockingCollection<HttpMessage>();

        public SoapCsvMessageLogger(string logFileName, TimeSpan inactivityLogTime)
        {
            this.logStream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 3);
            this.LogMessage(GetFieldNames());
            this.inactivityLogTime = inactivityLogTime;
            this.inactivityTimer = new Timer(OnInactivityTimer, null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            this.StartLoggingTask();
        }

        private static string GetTime(DateTime? date = null)
        {
            return (date ?? DateTime.Now).ToString(LogTimerFormat, CultureInfo.InvariantCulture);
        }

        private void OnInactivityTimer(object state)
        {
            var separator = new SoapCsvMessageLoggerFields 
            {
                Time = GetTime(),
                Location = "-----##------",
                SoapAction = "-----##------",
                RequestContentLength = "-----##------",
                ResponseContentLength = "-----##------",
                RequestMiliseconds = "-----##------" 
            };

            this.LogMessage(separator);
        }

        public void LogMessageFinished(HttpMessage message)
        {
            this.finishedMessagesQueue.Add(message);
        }

        private void StartLoggingTask()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var finishedMessage in finishedMessagesQueue.GetConsumingEnumerable())
                {
                    this.LogMessage(FormatMessage(finishedMessage));
                    this.inactivityTimer.Change(this.inactivityLogTime, TimeSpan.FromMilliseconds(-1));
                }
            });
        }

        private static SoapCsvMessageLoggerFields GetFieldNames()
        {
            return new SoapCsvMessageLoggerFields 
            {
                Time = "Time",
                Location = "Url",
                SoapAction = "SOAP Action",
                RequestContentLength = "Request size in bytes",
                ResponseContentLength = "Response size in bytes",
                RequestMiliseconds = "Request/response time in milliseconds"
            };
        }

        private void LogMessage(SoapCsvMessageLoggerFields data)
        {
            string messageText = string.Join(
                MessagePartSeparator,
                data.Time,
                data.Location, 
                data.SoapAction, 
                data.RequestContentLength,
                data.ResponseContentLength,
                data.RequestMiliseconds) + MessageSeparator;

            byte[] messageBytes = Encoding.UTF8.GetBytes(messageText);

            lock (syncLock)
            {
                this.logStream.Write(messageBytes, 0, messageBytes.Length);
                this.logStream.Flush();
            }
        }

        private static SoapCsvMessageLoggerFields FormatMessage(HttpMessage message)
        {
            return new SoapCsvMessageLoggerFields 
            { 
                //Time = GetTime(message.RequestStartTime),
                Location = message.Request.Location,
                //SoapAction = message.Request.SoapAction ?? message.Response.SoapAction,
                RequestContentLength = message.Request.LoadedContentLength.ToString(),
                ResponseContentLength = message.Response.LoadedContentLength.ToString(),
                //RequestMiliseconds = ((int)message.ElapsedTime.TotalMilliseconds).ToString()
            };
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.logStream != null)
            {
                this.logStream.Dispose();
            }

            if (this.inactivityTimer != null)
            {
                this.inactivityTimer.Dispose();
            }

            if (this.finishedMessagesQueue != null)
            {
                this.finishedMessagesQueue.Dispose();
            }
        }
    }
}
