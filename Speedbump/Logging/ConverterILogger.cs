namespace Speedbump
{
    public class ConverterILogger : Microsoft.Extensions.Logging.ILogger, IDisposable
    {
        private ILogger Logger;
        private string Source;
        private Lifetime Lifetime;
        public ConverterILogger(ILogger logger, string source, Lifetime lifetime)
        {
            Logger = logger;
            Source = source;
            Lifetime = lifetime;
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

            if (Source == "#Discord" && formatter is not null && formatter.Invoke(state, exception).Contains("connection is zombie") && level == LogLevel.Critical)
            {
                Logger.Information("Shutting down...");
                File.Delete("lock");
                Lifetime.End(Lifetime.ExitCause.Normal);
                Environment.Exit(0);
            }
        }
    }
}
