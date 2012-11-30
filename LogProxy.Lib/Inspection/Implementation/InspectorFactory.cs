namespace LogProxy.Lib.Inspection.Implementation
{
    public class InspectorFactory : IInspectorFactory
    {
        ProxySettings settings;

        public InspectorFactory(ProxySettings settings)
        {
            this.settings = settings;
        }

        public IServerConnectionInspector CreateServerConnectionInspector(string host, int port, bool secure)
        {
            return null;
        }

        public IHttpMessageInspector CreateHttpMessageInspector(Http.HttpMessage message)
        {
            return new AggregateHttpMessageInspector(new LoggingMessageInspector(this.settings));
        }
    }
}
