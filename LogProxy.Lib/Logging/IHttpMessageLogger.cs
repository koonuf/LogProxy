using System;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Logging
{
    public interface IHttpMessageLogger : IDisposable
    {
        void LogMessageFinished(HttpMessage message);
    }
}
