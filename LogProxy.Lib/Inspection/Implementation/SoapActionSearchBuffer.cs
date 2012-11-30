using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LogProxy.Lib.Streams;

namespace LogProxy.Lib.Inspection.Implementation
{
    public class SoapActionSearchBuffer : IDisposable
    {
        private const string SoapContentTypeValue = "APPLICATION/SOAP+XML";
        private const string SoapActionHeader = "SOAPACTION";
        private const string ContentTypeHeaderName = "CONTENT-TYPE";

        private const string AddressingNamespace = "http://www.w3.org/2005/08/addressing";
        private const string ActionTagName = "Action";

        public BlockingMemoryStream stream;
        private ManualResetEventSlim waitHandle;
        private volatile bool finished;
        private Stopwatch watch;

        public SoapActionSearchBuffer()
        {
            this.stream = new BlockingMemoryStream();
            this.waitHandle = new ManualResetEventSlim(false);
        }

        public void StartSearch()
        {
            Task.Factory.StartNew(this.ReadContent);
        }

        public void Wait()
        {
            this.waitHandle.Wait();
            this.watch.Stop();
        }

        public string SoapAction { get; private set; }

        private void ReadContent()
        {
            this.watch = Stopwatch.StartNew();

            using (var xmlReader = XmlReader.Create(this.stream))
            {
                try
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element &&
                            xmlReader.NamespaceURI == AddressingNamespace &&
                            xmlReader.LocalName == ActionTagName)
                        {
                            this.SoapAction = xmlReader.ReadElementContentAsString();
                            this.finished = true;
                            this.waitHandle.Set();
                            return;
                        }
                    }
                }
                catch (XmlException)
                {
                    this.finished = true;
                    this.waitHandle.Set();
                }
            }
        }


        private void Temp()
        {
            // string contentType = this.Headers[ContentTypeHeaderName].FirstOrDefault();

            //string soapAction = this.Headers[SoapActionHeader].FirstOrDefault();

            //if (soapAction != null ||
            //    (contentType != null && contentType.ToUpperInvariant().Contains(SoapContentTypeValue)))
            //{
            //    this.IsSoapMessage = true;
            //    this.SoapAction = soapAction;
            //}
        }

        public void AddContent(byte[] data, int offset, int count)
        {
            if (!this.finished)
            {
                this.stream.Write(data, offset, count);
            }
        }

        public void FinishContentTransfer()
        {
            this.stream.Finish();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.waitHandle != null)
            {
                this.waitHandle.Set();
                this.waitHandle.Dispose();
            }
        }
    }
}
