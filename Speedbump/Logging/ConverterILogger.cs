using Speedbump.Logging;

namespace Speedbump
{
    public class ConverterILogger : Microsoft.Extensions.Logging.ILogger, IDisposable
    {
        private ILogger Logger;
        private string Source;
        public ConverterILogger(ILogger logger, string source)
        {
            Logger = logger;
            Source = source;
        }

        public IDisposable BeginScope<TState>(TState state) { return this; }
        public void Dispose() { }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var level = (LogLevel)(int)logLevel;
            Logger.Log(level, $"#{Source} > {formatter?.Invoke(state, exception)}", 1);
            if (exception is not null)
            {
                Logger.Error(exception);
            }
        }
    }
}
