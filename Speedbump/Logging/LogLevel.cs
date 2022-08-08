namespace Speedbump
{
    public enum LogLevel
    {
        Trace = Microsoft.Extensions.Logging.LogLevel.Trace,
        Debug = Microsoft.Extensions.Logging.LogLevel.Debug,
        Information = Microsoft.Extensions.Logging.LogLevel.Information,
        Warning = Microsoft.Extensions.Logging.LogLevel.Warning,
        Error = Microsoft.Extensions.Logging.LogLevel.Error,
        Critical = Microsoft.Extensions.Logging.LogLevel.Critical,
        None = Microsoft.Extensions.Logging.LogLevel.None,
    }
}
