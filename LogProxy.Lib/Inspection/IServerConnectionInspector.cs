namespace LogProxy.Lib.Inspection
{
    public interface IServerConnectionInspector
    {
        void StartConnection();

        void ConnectionMade();

        void StartDnsResolve();

        void FinishDnsResolve();
    }
}
