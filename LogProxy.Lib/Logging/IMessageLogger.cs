namespace LogProxy.Lib.Logging
{
    public interface IMessageLogger
    {
        void LogMessage(MessageCategory category, MessageLevel level, string message);
    }
}
