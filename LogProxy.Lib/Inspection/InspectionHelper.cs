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

        public static void SafeServerReceiveFinished(this IMessageInspector messageInspector)
        {
            if (messageInspector != null)
            {
                messageInspector.ServerReceiveFinished();
            }
        }

        public static void SafeAddRequestData(this IMessageInspector messageInspector, byte[] data)
        {
            if (messageInspector != null)
            {
                messageInspector.AddRequestData(data);
            }
        }

        public static void SafeAddResponseData(this IMessageInspector messageInspector, byte[] data)
        {
            if (messageInspector != null)
            {
                messageInspector.AddResponseData(data);
            }
        }
    }
}
