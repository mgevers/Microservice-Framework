using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public sealed class FakeLoggingDatabase : IDisposable
{
    private static readonly AsyncLocal<List<LogEntry>?> asyncLocalLogs = new();
    private static readonly AsyncLocal<FakeLoggingConfiguration?> asyncLocalLogConfig = new();

    private FakeLoggingDatabase(FakeLoggingConfiguration loggingConfiguration)
    {
        asyncLocalLogs.Value = [];
        asyncLocalLogConfig.Value = loggingConfiguration;
    }

    public static FakeLoggingDatabase Initialize(FakeLoggingConfiguration? loggingConfiguration = null)
    {
        return new FakeLoggingDatabase(loggingConfiguration ?? new FakeLoggingConfiguration());
    }

    public static FakeLoggingDatabase Initialize(LogLevel defaultLogLevel)
    {
        var config = new FakeLoggingConfiguration(defaultLogLevel);

        return new FakeLoggingDatabase(config);
    }

    public static IReadOnlyList<LogEntry> Logs => asyncLocalLogs.Value?.ToList().AsReadOnly()
        ?? Array.Empty<LogEntry>() as IReadOnlyList<LogEntry>;

    public static FakeLoggingConfiguration? LoggingConfiguration => asyncLocalLogConfig.Value;

    public static void AddLog(LogEntry logEntry)
    {
        asyncLocalLogs.Value?.Add(logEntry);
    }

    public static bool IsInitialized()
    {
        return asyncLocalLogs.Value != null;
    }

    public void Dispose()
    {
        asyncLocalLogs.Value = null;
        asyncLocalLogConfig.Value = null;
    }
}
