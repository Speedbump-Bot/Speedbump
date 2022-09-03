using Microsoft.Extensions.Logging;

namespace Speedbump
{
    public class ConverterILoggerFactory : ILoggerFactory, ILoggerProvider
    {
        private ILogger Logger;
        private string Source;
        private Lifetime Lifetime;
        public ConverterILoggerFactory(ILogger logger, string source, Lifetime lifetime)
        {
            Logger = logger;
            Source = source;
            Lifetime = lifetime;
        }

        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new ConverterILogger(Logger, Source, Lifetime);

    }
}
