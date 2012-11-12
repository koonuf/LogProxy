using System;

namespace LogProxy.Lib.Logging
{
    public interface IHttpMessageLogger : IDisposable
    {
        void LogMessageFinished(HttpMessage message);
    }
}
