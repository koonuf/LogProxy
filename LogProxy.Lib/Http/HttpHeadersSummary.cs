using System.Collections.Generic;
using System.Linq;

namespace LogProxy.Lib.Http
{
    public class HttpHeadersSummary
    {
        public IList<string> StatusLine { get; set; }

        public ILookup<string, string> Headers { get; set; }

        public int? ContentLength { get; set; }

        public int HeaderLength { get; set; }

        public string HeadersContent { get; set; }
    }
}
