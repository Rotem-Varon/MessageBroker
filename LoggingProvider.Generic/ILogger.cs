namespace LoggingProvider.Generic
{
    public interface ILogger
    {
        void LogInfo(string msg);
        void LogError(string msg);
        void LogWarning(string msg);
    }
}
