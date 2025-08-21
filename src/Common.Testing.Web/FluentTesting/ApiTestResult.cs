using Common.Testing.FluentTesting;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;

namespace Common.Testing.Integration.FluentTesting;

public class ApiTestResult<T> : IRepsitoryTestResult, IServiceBusTestResult, ITestOutput<T>, ILoggingTestResult
{
    public ApiTestResult(
        DatabaseState databaseState,
        ServiceBusState serviceBusMessages,
        IReadOnlyCollection<LogEntry> logs,
        T output)
    {
        DatabaseState = databaseState;
        ServiceBusState = serviceBusMessages;
        Logs = logs.ToList();
        Output = output;
    }

    public DatabaseState DatabaseState { get; }
    public ServiceBusState ServiceBusState { get; }
    public T Output { get; }

    public IReadOnlyList<LogEntry> Logs { get; }
}
