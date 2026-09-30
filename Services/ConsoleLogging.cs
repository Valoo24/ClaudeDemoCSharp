using Microsoft.Extensions.Logging;

namespace ClaudeDemo.Services;

// One console logger setup shared by the scenarios that use a service. The built-in console logger
// writes on a background thread, so its lines can show up after the console output that follows them;
// this one writes immediately, keeping the log in the order things happen.
static class ConsoleLogging
{
    static readonly ILoggerFactory Factory = LoggerFactory.Create(logging =>
        logging.SetMinimumLevel(LogLevel.Information).AddProvider(new SynchronousConsoleLoggerProvider())
    );

    public static ILogger<T> Create<T>() => Factory.CreateLogger<T>();

    sealed class SynchronousConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new SynchronousConsoleLogger();

        public void Dispose() { }
    }

    sealed class SynchronousConsoleLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (IsEnabled(logLevel))
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {logLevel.ToString().ToLowerInvariant()}: {formatter(state, exception)}");
            }
        }
    }
}
