using LogProxy.Lib.Http;
namespace LogProxy.Lib.Inspection
{
    public static class InspectionHelper
    {
        public static void SafeStartConnection(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.StartConnection();
            }
        }

        public static void SafeStartDnsResolve(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.StartDnsResolve();
            }
        }

        public static void SafeFinishDnsResolve(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.FinishDnsResolve();
            }
        }

        public static void SafeConnectionMade(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.ConnectionMade();
            }
        }

        public static void SafeServerReceiveFinished(this IHttpMessageInspector messageInspector)
        {
            if (messageInspector != null)
            {
                messageInspector.ServerReceiveFinished();
            }
        }

        public static void SafeAddRequestData(this IHttpMessageInspector messageInspector, byte[] data)
        {
            if (messageInspector != null)
            {
                messageInspector.AddRequestData(data);
            }
        }

        public static void SafeAddResponseData(this IHttpMessageInspector messageInspector, byte[] data)
        {
            if (messageInspector != null)
            {
                messageInspector.AddResponseData(data);
            }
        }

        public static void SafeRequestHeadersParsed(this IHttpMessageInspector messageInspector, HttpHeadersSummary headers)
        {
            if (messageInspector != null)
            {
                messageInspector.RequestHeadersParsed(headers);
            }
        }

        public static void SafeResponseHeadersParsed(this IHttpMessageInspector messageInspector, HttpHeadersSummary headers)
        {
            if (messageInspector != null)
            {
                messageInspector.ResponseHeadersParsed(headers);
            }
        }
    }
}
