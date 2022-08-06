using Microsoft.Extensions.Logging;

namespace Speedbump
{
    public class ConverterILoggerFactory : ILoggerFactory, ILoggerProvider
    {
        private ILogger Logger;
        private string Source;
        public ConverterILoggerFactory(ILogger logger, string source)
        {
            Logger = logger;
            Source = source;
        }

        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new ConverterILogger(Logger, Source);

    }
}
