using Microsoft.Extensions.Logging;

namespace MiUtils.Plugin;

public class Logger<T>(MiLoggerProvider provider, string categoryName) : ILogger<T>
{
    private readonly object _lock = new object();
    private readonly MiLoggerProvider _provider = provider;
    private readonly string _categoryName = categoryName;
    private void LogMessage(LogLevel logLevel, string message, ConsoleColor? bodyColor = null)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        lock (_lock)
        {
            Console.ForegroundColor = GetLogLevelColor(logLevel);
            Console.Write($"[MiFramework-{logLevel}-{DateTime.Now}] {typeof(T).Name} ");

            if (bodyColor.HasValue)
            {
                Console.ForegroundColor = bodyColor.Value;
            }

            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    private ConsoleColor GetLogLevelColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Information => ConsoleColor.Gray,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Debug => ConsoleColor.Green,
            LogLevel.Trace => ConsoleColor.Cyan,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White,
        };
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logMessage = formatter(state, exception);
        _provider.Log(logMessage);
        LogMessage(logLevel, logMessage);
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
    public void LogAdvanced<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter, ConsoleColor bodyColor)
    {
        var logMessage = formatter(state, exception);
        _provider.Log(logMessage);
        LogMessage(logLevel, logMessage);
        LogMessage(logLevel, logMessage, bodyColor);
    }
}

