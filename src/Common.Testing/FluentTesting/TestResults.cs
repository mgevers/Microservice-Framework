using Ardalis.Result;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Moq.AutoMock;

namespace Common.Testing.FluentTesting;

public class MessageHandlerTestResult<THandler> :
    IRepsitoryTestResult,
    IServiceBusTestResult,
    IAutoMockerTestResult,
    IExceptionTestResult
{
    public MessageHandlerTestResult(
        DatabaseState databaseState,
        ServiceBusState serviceBusMessages,
        AutoMocker autoMocker,
        Exception? exceptionThrown = null)
    {
        DatabaseState = databaseState;
        ServiceBusState = serviceBusMessages;
        AutoMocker = autoMocker;
        ExceptionThrown = exceptionThrown;
    }

    public DatabaseState DatabaseState { get; }
    public ServiceBusState ServiceBusState { get; }
    public AutoMocker AutoMocker { get; }
    public Exception? ExceptionThrown { get; }
}

public class RequestHandlerTestResult<THandler> :
    IRepsitoryTestResult,
    IServiceBusTestResult,
    IAutoMockerTestResult,
    ITestOutput<Result>,
    IExceptionTestResult
{
    public RequestHandlerTestResult(
        DatabaseState databaseState,
        ServiceBusState serviceBusMessages,
        AutoMocker autoMocker,
        Result output,
        Exception? exceptionThrown = null)
    {
        DatabaseState = databaseState;
        ServiceBusState = serviceBusMessages;
        AutoMocker = autoMocker;
        Output = output;
        ExceptionThrown = exceptionThrown;
    }

    public DatabaseState DatabaseState { get; }
    public ServiceBusState ServiceBusState { get; }
    public AutoMocker AutoMocker { get; }
    public Result Output { get; }
    public Exception? ExceptionThrown { get; }
}
