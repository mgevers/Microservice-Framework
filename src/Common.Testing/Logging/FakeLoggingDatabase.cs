namespace Common.Testing.Logging;

public class FakeLoggingDatabase : IDisposable
{
    private static readonly AsyncLocal<List<LogEntry>?> asyncLocalLogs = new();

    private FakeLoggingDatabase()
    {
        asyncLocalLogs.Value = new List<LogEntry>();
    }

    public static FakeLoggingDatabase Initialize()
    {
        return new FakeLoggingDatabase();
    }

    public static IReadOnlyList<LogEntry> Logs => asyncLocalLogs.Value?.ToList().AsReadOnly()
        ?? Array.Empty<LogEntry>() as IReadOnlyList<LogEntry>;

    public static void AddLog(LogEntry logEntry)
    {
        asyncLocalLogs.Value?.Add(logEntry);
    }

    public void Dispose()
    {
        asyncLocalLogs.Value = null;
    }
}
