using Speedbump.Logging;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace Speedbump
{
    public class Logger : ILogger
    {
        public class LogMessage
        {
            public LogLevel Level { get; set; }
            public object Content { get; set; }
            public string File { get; set; }
            public int Line { get; set; }
            public DateTime Time { get; set; }
        }

        private readonly IConfiguration Configuration;
        private ConcurrentQueue<LogMessage> LogQueue = new();
        private string CurrentFile;
        private bool IsLogging;
        public List<Action<LogMessage>> LogHandlers = new();
        Dictionary<LogLevel, ConsoleColor> colors = new()
            {
                { LogLevel.Trace, ConsoleColor.Gray },
                { LogLevel.Debug, ConsoleColor.Magenta },
                { LogLevel.Information, ConsoleColor.White },
                { LogLevel.Warning, ConsoleColor.Yellow },
                { LogLevel.Error, ConsoleColor.Red },
                { LogLevel.Critical, ConsoleColor.DarkRed },
            };

        public Logger(IConfiguration config, Lifetime lifetime)
        {
            Configuration = config;
            lifetime.Add(End, Lifetime.ExitOrder.Logging);

            // Init logging directory
            var logDir = config.Get<string>("logging.directory");
            if (!Directory.Exists(logDir)) { Directory.CreateDirectory(logDir); }

            // Compress Old Logs
            var count = 0;
            foreach (var file in Directory.GetFiles(logDir))
            {
                if (Path.GetExtension(file) == ".zip") { continue; }
                var newFile = Path.GetFileName(file) + ".zip";
                var tempFolder = Directory.CreateDirectory(Path.Combine(logDir, "temp-" + Path.GetFileNameWithoutExtension(file)));
                File.Move(file, Path.Combine(tempFolder.FullName, Path.GetFileName(file)));
                ZipFile.CreateFromDirectory(tempFolder.FullName, Path.Combine(logDir, newFile));
                Directory.Delete(tempFolder.FullName, true);
                count++;
            }

            // Init current log file
            CurrentFile = Path.Combine($"{logDir}", $"{Snowflake.Generate()}.log");
            File.Create(CurrentFile).Dispose();

            // Init handlers
            LogHandlers.Add(Handler_Console);
            LogHandlers.Add(Handler_File);

            // Begin logging
            new Thread(LogThread).Start();
            Log(LogLevel.Information, $"Logger online. Zipped {count} old logs.");
        }

        public void Log(LogLevel level, object message, int stack = 2)
        {
            var frame = new StackFrame(stack, true);
            var file = frame.GetFileName();
            file = file is null ? "" : file.Contains('\\') ? file[(file.LastIndexOf(@"\") + 1)..] : file;
            var line = frame.GetFileLineNumber();

            LogQueue.Enqueue(new LogMessage()
            {
                Level = level,
                Content = message,
                Line = line,
                File = file,
                Time = DateTime.Now,
            });
        }

        private void LogThread()
        {
            while (true)
            {

                while (LogQueue.IsEmpty) { Thread.Sleep(1); }
                if (LogQueue.TryDequeue(out var msg))
                {
                    IsLogging = true;
                    LogHandlers.ForEach(handler => handler?.Invoke(msg));
                    IsLogging = false;
                }
            }
        }

        private void Handler_Console(LogMessage msg)
        {
            if (msg.Level < Enum.Parse<LogLevel>(Configuration.Get<string>("logging.levels.console"))) { return; }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(msg.Time.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.ForegroundColor = colors[msg.Level];
            var type = msg.Level.ToString();
            type = type.Length > 4 ? type.Substring(0, 5) : type;
            Console.Write($" {type,-5} ");
            Console.Write($"{msg.File + ":" + msg.Line,-30} - {msg.Content}\n\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Handler_File(LogMessage msg)
        {
            if (msg.Level < Enum.Parse<LogLevel>(Configuration.Get<string>("logging.levels.file"))) { return; }

            var message = $"{msg.Time:yyyy-MM-dd HH:mm:ss} {msg.Level} {msg.File}:{msg.Line} - {msg.Content}\n";
            File.AppendAllText(CurrentFile, message);
        }

        private void End(Lifetime.ExitCause cause)
        {
            Log(LogLevel.Information, $"Logger shutting down ({cause})...");
            while (!LogQueue.IsEmpty || IsLogging) { Thread.Sleep(1); }
        }
    }
}
