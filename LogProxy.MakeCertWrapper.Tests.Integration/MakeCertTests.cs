using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogProxy.MakeCertWrapper.Tests.Integration
{
    [TestClass]
    public class MakeCertTests
    {
        [TestMethod]
        public void Test()
        {
            var provider = new CertificateProvider(@"C:\Program Files (x86)\Fiddler2\makecert.exe", null);

            provider.EnsureRootCertificate();
            var certificate = provider.GetCertificateForHost("www.test3.com");
        }
    }
}
