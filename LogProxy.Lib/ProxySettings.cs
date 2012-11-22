using LogProxy.Lib.Inspection;

namespace LogProxy.Lib
{
    public class ProxySettings
    {
        public int ListenPort { get; set; }

        public string MessageBodyLogDirectory { get; set; }

        public bool LogMessageBody { get; set; }

        public IInspectorFactory InspectorFactory { get; set; }

        public ICertificateProvider CertificateProvider { get; set; }
    }
}
