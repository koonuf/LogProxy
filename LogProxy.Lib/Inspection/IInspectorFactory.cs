namespace LogProxy.Lib.Inspection
{
    public interface IInspectorFactory
    {
        IServerConnectionInspector CreateServerConnectionInspector(string host, int port, bool secure);

        IMessageInspector CreateMessageInspector(HttpMessage message);
    }
}
