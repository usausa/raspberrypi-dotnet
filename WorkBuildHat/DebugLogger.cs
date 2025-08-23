#pragma warning disable IDE0130
using Microsoft.Extensions.Logging;

internal sealed class DebugLogger : ILogger
{
    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
            // Do nothing
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (exception is not null)
        {
            message += Environment.NewLine + Environment.NewLine + exception;
        }

        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
    }
}

internal sealed class DebugLoggerFactory : ILoggerFactory
{
    public void Dispose()
    {
        // Do nothing
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // Do nothing
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DebugLogger();
    }
}
