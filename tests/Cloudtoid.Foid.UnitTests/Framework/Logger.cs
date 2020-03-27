namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal class Logger<T> : ILogger<T>
    {
        public Logger(ILoggerFactory factory)
        {
            CheckValue(factory, nameof(factory));
        }

        internal IList<string> Logs { get; } = new List<string>();

        IDisposable ILogger.BeginScope<TState>(TState state)
            => NoOpDisposable.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel)
            => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logs.Add(formatter(state, exception));
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new NoOpDisposable();

            private NoOpDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}