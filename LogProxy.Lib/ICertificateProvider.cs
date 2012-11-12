using System.Security.Cryptography.X509Certificates;

namespace LogProxy.Lib
{
    public interface ICertificateProvider
    {
        void EnsureRootCertificate();

        X509Certificate2 GetCertificateForHost(string host);
    }
}
