using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LogProxy.Lib
{
    public static class DnsCacheReader
    {
        public static void ReadDnsCache(string filename, IDictionary<string, IPAddress> cache)
        {
            var doc = XDocument.Load(filename);
            foreach (var entry in doc.XPathSelectElements("/cache/entry")
                        .ToDictionary(e => (string)e.Attribute("host"), e => IPAddress.Parse((string)e.Attribute("ip"))))
            {
                cache.Add(entry.Key.ToUpperInvariant(), entry.Value);
            }
        }
    }
}
