using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PerfCompare.Server.Tests;

internal sealed record LogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

internal sealed class LogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new CollectingLogger(categoryName, _entries);
    }

    public void Dispose()
    {
        // nothing to release
    }

    private sealed class CollectingLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<LogEntry> _entries;

        public CollectingLogger(string category, ConcurrentQueue<LogEntry> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(new LogEntry(_category, logLevel, formatter(state, exception), exception));
        }
    }
}
