using System;
using System.Collections.Generic;

namespace LogProxy.Lib.Inspection.Implementation
{
    public class AggregateHttpMessageInspector : IHttpMessageInspector
    {
        private IEnumerable<IHttpMessageInspector> inspectors;

        public AggregateHttpMessageInspector(IEnumerable<IHttpMessageInspector> inspectors)
        {
            this.inspectors = inspectors;
        }

        public AggregateHttpMessageInspector(params IHttpMessageInspector[] inspectors)
        {
            this.inspectors = inspectors;
        }

        public void AddRequestData(byte[] data)
        {
            this.Apply(i => i.AddRequestData(data));
        }

        public void AddResponseData(byte[] data)
        {
            this.Apply(i => i.AddResponseData(data));
        }

        public void RequestHeadersParsed(Http.HttpHeadersSummary headers)
        {
            this.Apply(i => i.RequestHeadersParsed(headers));
        }

        public void ResponseHeadersParsed(Http.HttpHeadersSummary headers)
        {
            this.Apply(i => i.ResponseHeadersParsed(headers));
        }

        public void ServerReceiveFinished()
        {
            this.Apply(i => i.ServerReceiveFinished());
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Apply(i => i.Dispose());
        }

        private void Apply(Action<IHttpMessageInspector> action)
        {
            foreach (var inspector in this.inspectors)
            {
                action(inspector);
            }
        }
    }
}
