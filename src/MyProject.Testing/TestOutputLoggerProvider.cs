using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MyProject.Testing
{
    /// <summary>
    /// This provider allows us to capture log messages and show them for each xUnit test
    /// </summary>
    internal class TestOutputLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;
        private readonly ConcurrentDictionary<string, TestOutputLogger> _loggers = new ConcurrentDictionary<string, TestOutputLogger>();

        /// <summary>
        /// All the entries that get logged
        /// </summary>
        public List<LogEntry> Entries { get; } = new List<LogEntry>();

        public TestOutputLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, _ => new TestOutputLogger(_output, categoryName, Entries));
        }

        public void Dispose()
        {
            Entries.Clear();
            _loggers.Clear();
        }

        public void VerifyLog(LogLevel logLevel, string? message = null, string? category = null, int? count = null)
        {
            lock (Entries)
            {
                var byLogLevel = Entries.Where(l => l.LogLevel == logLevel);

                if (string.IsNullOrEmpty(message) == false)
                {
                    byLogLevel = byLogLevel.Where(l => l.Message.Contains(message));
                }

                if (string.IsNullOrEmpty(category) == false)
                {
                    byLogLevel = byLogLevel.Where(l => l.Category.Contains(category));
                }

                if (count == null)
                {
                    Assert.NotNull(byLogLevel.FirstOrDefault());
                }
                else
                {
                    Assert.Equal(count, byLogLevel.Count());
                }
            }
        }
    }

    internal class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _category;
        private readonly List<LogEntry> _entries;

        public TestOutputLogger(ITestOutputHelper output, string category, List<LogEntry> entries)
        {
            _output = output;
            _category = category;
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_entries)
            {
                var formattetMessage = formatter(state, exception);
                _entries.Add(new LogEntry
                {
                    Category = _category,
                    LogLevel = logLevel,
                    Message = formattetMessage
                });

                // show in xUnit logs
                try
                {
                    _output?.WriteLine(formattetMessage);
                    if (exception != null)
                    {
                        _output?.WriteLine(exception.Message);
                        _output?.WriteLine(exception.StackTrace);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }
    }

    internal class LogEntry
    {
        [NotNull, DisallowNull]
        public string? Category { get; set; }
        public LogLevel LogLevel { get; set; }
        [NotNull, DisallowNull]
        public string? Message { get; set; }
    }
}
