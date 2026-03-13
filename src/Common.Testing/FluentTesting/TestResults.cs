using Ardalis.Result;
using Common.Testing.Logging;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class MessageHandlerTestResult<THandler> :
    IRepsitoryTestResult,
    IServiceBusTestResult,
    IAutoMockerTestResult,
    IExceptionTestResult,
    ILoggingTestResult
{
    public MessageHandlerTestResult(
        DatabaseState databaseState,
        ServiceBusState serviceBusMessages,
        AutoMocker autoMocker,
        IReadOnlyList<LogEntry> logs,
        Exception? exceptionThrown = null)
    {
        DatabaseState = databaseState;
        ServiceBusState = serviceBusMessages;
        AutoMocker = autoMocker;
        Logs = logs;
        ExceptionThrown = exceptionThrown;
    }

    public DatabaseState DatabaseState { get; }
    public ServiceBusState ServiceBusState { get; }
    public AutoMocker AutoMocker { get; }
    public Exception? ExceptionThrown { get; }

    public IReadOnlyList<LogEntry> Logs { get; }
}

public class RequestHandlerTestResult<THandler> :
    IRepsitoryTestResult,
    IServiceBusTestResult,
    IAutoMockerTestResult,
    ITestOutput<Result>,
    IExceptionTestResult,
    ILoggingTestResult
{
    public RequestHandlerTestResult(
        DatabaseState databaseState,
        ServiceBusState serviceBusMessages,
        AutoMocker autoMocker,
        Result output,
        IReadOnlyList<LogEntry> logs,
        Exception? exceptionThrown = null)
    {
        DatabaseState = databaseState;
        ServiceBusState = serviceBusMessages;
        AutoMocker = autoMocker;
        Output = output;
        Logs = logs;
        ExceptionThrown = exceptionThrown;
    }

    public DatabaseState DatabaseState { get; }
    public ServiceBusState ServiceBusState { get; }
    public AutoMocker AutoMocker { get; }
    public Result Output { get; }
    public Exception? ExceptionThrown { get; }

    public IReadOnlyList<LogEntry> Logs { get; }
}
