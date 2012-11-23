using System;
using LogProxy.Lib.Http;

namespace LogProxy.Lib.Inspection
{
    public interface IHttpMessageInspector : IDisposable
    {
        void AddRequestData(byte[] data);

        void AddResponseData(byte[] data);

        void RequestHeadersParsed(HttpHeadersSummary headers);

        void ResponseHeadersParsed(HttpHeadersSummary headers);

        void ServerReceiveFinished();
    }
}
