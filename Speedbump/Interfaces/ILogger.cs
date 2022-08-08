namespace Speedbump
{
    public interface ILogger
    {
        public void Log(LogLevel level, object message, int stack = 2);
        public void Trace(object content) => Log(LogLevel.Trace, content);
        public void Debug(object content) => Log(LogLevel.Debug, content);
        public void Information(object content) => Log(LogLevel.Information, content);
        public void Warning(object content) => Log(LogLevel.Warning, content);
        public void Error(object content) => Log(LogLevel.Error, content);
        public void Critical(object content) => Log(LogLevel.Critical, content);
        public void AddRedacted(string content);
    }
}
