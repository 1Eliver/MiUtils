using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MiUtils.Plugin;

public class MiLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<string> _logMessages = [];
    private readonly string _logDirectory;

    public MiLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger<MiLoggerProvider>(this, categoryName);
    }

    public void Dispose()
    {
        // 在这里实现日志保存到文件逻辑
        var logFileName = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd_HHmmss}.log");
        File.WriteAllLines(logFileName, _logMessages);
    }

    public void Log(string message)
    {
        _logMessages.Add(message);
    }
}