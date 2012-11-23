using LogProxy.Lib.Http;

namespace LogProxy.Lib.Inspection
{
    public interface IInspectorFactory
    {
        IServerConnectionInspector CreateServerConnectionInspector(string host, int port, bool secure);

        IHttpMessageInspector CreateHttpMessageInspector(HttpMessage message);
    }
}
