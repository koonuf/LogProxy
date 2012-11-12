using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogProxy.Lib.Tests.Unit
{
    [TestClass]
    public class SoapActionSearchBufferTests
    {
        [TestMethod]
        public void TestSearch()
        {
            for (int i = 0; i < 20; i++)
            {
                PerformTest();
            }
        }

        private static void PerformTest()
        {
            string content = "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\"><s:Header><a:Action s:mustUnderstand=\"1\">http://schemas.ncc.se/roads/services/entity/agreement/1/0/AgreementServicePortType/GetByUserIdResponse</a:Action><a:MessageID>urn:uuid:6b76c5b2-5252-42ac-8c6a-2029098a518d</a:MessageID><a:RelatesTo>urn:uuid:d6d0571d-cc23-4b4f-8900-98dfb4b4356d</a:RelatesTo></s:Header><s:Body></s:Body></s:Envelope>";
            var binContent = Encoding.UTF8.GetBytes(content);

            var search = new SoapActionSearchBuffer();
            search.StartSearch();

            Task.Factory.StartNew(() =>
            {
                var step = 20;
                for (var i = 0; i < binContent.Length; i += step)
                {
                    var random = new Random((int)DateTime.Now.Ticks);
                    Thread.Sleep(random.Next(1, 4));
                    int count = Math.Min(binContent.Length - i, step);
                    search.AddContent(binContent, i, count);
                }

                search.FinishContentTransfer();
            });

            search.Wait();

            string expected = "http://schemas.ncc.se/roads/services/entity/agreement/1/0/AgreementServicePortType/GetByUserIdResponse";
            Assert.AreEqual(expected, search.SoapAction);
        }
    }
}
