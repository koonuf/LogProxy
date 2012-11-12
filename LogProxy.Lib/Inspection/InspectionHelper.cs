namespace LogProxy.Lib.Inspection
{
    public static class InspectionHelper
    {
        public static void SignalStartConnection(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.StartConnection();
            }
        }

        public static void SignalStartDnsResolve(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.StartDnsResolve();
            }
        }

        public static void SignalFinishDnsResolve(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.FinishDnsResolve();
            }
        }

        public static void SignalConnectionMade(this IServerConnectionInspector connectionInspector)
        {
            if (connectionInspector != null)
            {
                connectionInspector.ConnectionMade();
            }
        }
    }
}
